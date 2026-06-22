using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WorkTracker.Services
{
    /// <summary>Calendar event fetched from an .ics feed.</summary>
    public class IcsEventItem
    {
        public string   Subject  { get; set; } = string.Empty;
        public DateTime Start    { get; set; }
        public DateTime End      { get; set; }
        public string   Category { get; set; } = "Meeting";
        public bool     IsAllDay { get; set; }
        public bool     IsTransparent { get; set; }
        public string   Uid      { get; set; } = string.Empty;
    }

    /// <summary>
    /// Downloads and parses an iCalendar (.ics) feed from a URL.
    /// Works with Outlook.com shareable calendar links — no authentication needed.
    /// </summary>
    public static class IcsCalendarService
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

        /// <summary>
        /// Downloads the .ics file from <paramref name="icsUrl"/> and returns events
        /// whose start time falls within [<paramref name="from"/>, <paramref name="to"/>).
        /// Returns an empty list on any error (network, parse, etc.).
        /// </summary>
        public static async Task<List<IcsEventItem>> GetCalendarEventsAsync(
            string icsUrl, DateTime from, DateTime to)
        {
            var events = new List<IcsEventItem>();

            if (string.IsNullOrWhiteSpace(icsUrl))
            {
                Debug.WriteLine("[IcsCalendar] No ICS URL configured.");
                return events;
            }

            try
            {
                string icsContent = await _http.GetStringAsync(icsUrl).ConfigureAwait(false);
                var parsed = ParseIcs(icsContent);

                foreach (var e in parsed)
                {
                    if (e.IsTransparent)                     continue;   // skip events marked available (free)
                    if (e.Start == DateTime.MinValue)        continue;
                    if (e.End <= e.Start)                    continue;   // sanity
                    if (e.Start < from || e.Start >= to)     continue;

                    events.Add(e);
                }

                Debug.WriteLine($"[IcsCalendar] Fetched {parsed.Count} total, {events.Count} in range.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IcsCalendar] Fetch/parse failed: {ex.Message}");
            }

            return events;
        }

        // ──────────────────────────────────────────────────────────────────────
        // ICS Parser
        // ──────────────────────────────────────────────────────────────────────

        private static List<IcsEventItem> ParseIcs(string raw)
        {
            // 1. Unfold long lines (RFC 5545 §3.1 — continuation lines start with a space or tab)
            var unfolded = new StringBuilder(raw.Length);
            using (var reader = new System.IO.StringReader(raw))
            {
                string? line;
                string? prev = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (prev == null) { prev = line; continue; }

                    if (line.Length > 0 && (line[0] == ' ' || line[0] == '\t'))
                    {
                        // Continuation — append to previous line (strip leading whitespace char)
                        prev += line.Substring(1);
                    }
                    else
                    {
                        unfolded.AppendLine(prev);
                        prev = line;
                    }
                }
                if (prev != null) unfolded.AppendLine(prev);
            }

            var events = new List<IcsEventItem>();
            IcsEventItem? current = null;

            foreach (var raw_line in unfolded.ToString()
                         .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = raw_line.TrimEnd();

                if (trimmed == "BEGIN:VEVENT")
                {
                    current = new IcsEventItem();
                    continue;
                }

                if (trimmed == "END:VEVENT")
                {
                    if (current != null) events.Add(current);
                    current = null;
                    continue;
                }

                if (current == null) continue;

                // Split on the first colon — property name (with optional params) : value
                int colon = trimmed.IndexOf(':');
                if (colon < 0) continue;

                string propFull = trimmed.Substring(0, colon);
                string value    = trimmed.Substring(colon + 1);

                // Separate property name from parameters  e.g. DTSTART;TZID=Europe/Oslo
                int semi     = propFull.IndexOf(';');
                string prop  = (semi >= 0 ? propFull.Substring(0, semi) : propFull).ToUpperInvariant();
                string param = semi >= 0 ? propFull.Substring(semi + 1).ToUpperInvariant() : "";

                switch (prop)
                {
                    case "UID":
                        current.Uid = value.Trim();
                        break;

                    case "SUMMARY":
                        current.Subject = UnescapeIcs(value);
                        break;

                    case "DTSTART":
                        current.IsAllDay = param.Contains("VALUE=DATE") || (value.Length == 8);
                        current.Start    = ParseIcsDateTime(value, param);
                        break;

                    case "DTEND":
                        current.End = ParseIcsDateTime(value, param);
                        break;

                    case "DURATION":
                        // e.g. PT1H30M — apply to Start to get End
                        if (current.Start != DateTime.MinValue)
                            current.End = current.Start.Add(ParseDuration(value));
                        break;

                    case "CATEGORIES":
                        current.Category = DetermineCategory(current.Subject, value);
                        break;

                    case "TRANSP":
                        current.IsTransparent = value.Trim().Equals("TRANSPARENT", StringComparison.OrdinalIgnoreCase);
                        break;
                }
            }

            return events;
        }

        private static DateTime ParseIcsDateTime(string value, string param)
        {
            value = value.Trim();
            bool isUtc = value.EndsWith("Z", StringComparison.Ordinal);
            if (isUtc) value = value.Substring(0, value.Length - 1);

            // Date-only: YYYYMMDD (8 chars)
            if (value.Length == 8)
            {
                if (DateTime.TryParseExact(value, "yyyyMMdd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    return d;
            }
            // DateTime: YYYYMMDDTHHmmss (15 chars)
            else if (value.Length >= 15)
            {
                string fmt = "yyyyMMddTHHmmss";
                if (DateTime.TryParseExact(value.Substring(0, 15), fmt,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    // Convert UTC to local; TZID-tagged times are already "local" per that tz
                    // For simplicity: UTC → local; everything else treated as local
                    return isUtc
                        ? DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime()
                        : dt;
                }
            }

            Debug.WriteLine($"[IcsCalendar] Could not parse datetime: '{value}'");
            return DateTime.MinValue;
        }

        private static TimeSpan ParseDuration(string value)
        {
            // Very simple DURATION parser: PT1H, PT30M, P1DT2H, etc.
            try
            {
                // Strip leading P
                value = value.TrimStart('P', 'p');
                int days = 0, hours = 0, minutes = 0, seconds = 0;
                int num = 0;
                foreach (char c in value)
                {
                    if (c == 'T') { continue; }
                    if (char.IsDigit(c)) { num = num * 10 + (c - '0'); continue; }
                    switch (char.ToUpperInvariant(c))
                    {
                        case 'D': days    = num; break;
                        case 'H': hours   = num; break;
                        case 'M': minutes = num; break;
                        case 'S': seconds = num; break;
                    }
                    num = 0;
                }
                return new TimeSpan(days, hours, minutes, seconds);
            }
            catch { return TimeSpan.Zero; }
        }

        private static string UnescapeIcs(string value) =>
            value.Replace("\\n", "\n").Replace("\\N", "\n")
                 .Replace("\\,", ",").Replace("\\;", ";")
                 .Replace("\\\\", "\\");

        private static string DetermineCategory(string subject, string icsCategories)
        {
            // Check Outlook/Exchange category tags first
            if (!string.IsNullOrEmpty(icsCategories))
            {
                string lc = icsCategories.ToLowerInvariant();
                if (lc.Contains("work") || lc.Contains("task") || lc.Contains("project") || lc.Contains("focus"))
                    return "Offline Work";
                if (lc.Contains("meeting") || lc.Contains("sync") || lc.Contains("call"))
                    return "Meeting";
            }

            // Keyword matching on subject
            string sub = subject.ToLowerInvariant();
            if (sub.Contains("standup")  || sub.Contains("sync")      || sub.Contains("meeting") ||
                sub.Contains("review")   || sub.Contains("interview")  || sub.Contains("call")    ||
                sub.Contains("1:1")      || sub.Contains("planning")   || sub.Contains("retro")   ||
                sub.Contains("workshop") || sub.Contains("onboarding") || sub.Contains("demo"))
                return "Meeting";

            // Default: calendar events are meetings
            return "Meeting";
        }
    }
}
