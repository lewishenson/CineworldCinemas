﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using FluentCineworld.Utilities;

namespace FluentCineworld.OldListings
{
    [Obsolete("Do not use, site scrapping no longer works.")]
    public class SiteListingsScraper : IScraper<IEnumerable<Film>>
    {
        private readonly IWebClient _webClient;

        public SiteListingsScraper(IWebClient webClient)
        {
            _webClient = webClient;
        }

        public IEnumerable<Film> Scrape(Cinema cinema)
        {
            var uri = UriGenerator.WhatsOn(cinema);
            var content = _webClient.GetContent(uri);
            if (string.IsNullOrWhiteSpace(content))
            {
                return Enumerable.Empty<Film>();
            }

            var parser = new WhatsOnParser();
            var films = parser.Parse(content);

            return films;
        }

        private class WhatsOnParser
        {
            private readonly FilmParser _filmParser = new FilmParser();

            public IEnumerable<Film> Parse(string content)
            {
                var filmsStart = content.IndexOf("<!-- SEE WHAT'S ON START -->", StringComparison.InvariantCultureIgnoreCase);
                var filmsEnd = content.IndexOf("<!-- SEE WHAT'S ON END -->", filmsStart, StringComparison.InvariantCultureIgnoreCase);

                var filmsSubString = content.Substring(filmsStart, filmsEnd - filmsStart);
                var filmSubStrings = filmsSubString.Split(new[] { "<div class=\"mix " }, StringSplitOptions.RemoveEmptyEntries);

                var films = new List<Film>();

                foreach (var filmSubString in filmSubStrings)
                {
                    var film = _filmParser.Parse(filmSubString);
                    if (film != null)
                    {
                        films.Add(film);
                    }
                }

                return films;
            }
        }

        private class FilmParser
        {
            private readonly DaysParser _daysParser = new DaysParser();

            public Film Parse(string content)
            {
                var filmTitleStart = content.IndexOf("<h3 class=\"h1\">", StringComparison.InvariantCultureIgnoreCase);
                if (filmTitleStart < 0)
                {
                    return null;
                }

                var film = new Film();

                try
                {
                    filmTitleStart = content.IndexOf(">", filmTitleStart + 15, StringComparison.InvariantCultureIgnoreCase) + 1;

                    var filmTitleEnd = content.IndexOf("<", filmTitleStart, StringComparison.InvariantCultureIgnoreCase);

                    var rawFilmTitle = content.Substring(filmTitleStart, filmTitleEnd - filmTitleStart);
                    film.Title = TextFormatter.FormatTitle(rawFilmTitle.Trim());

                    if (string.IsNullOrWhiteSpace(film.Title))
                    {
                        return null;
                    }

                    var certificationStart = content.IndexOf("<a class=\"classification", filmTitleEnd, StringComparison.InvariantCultureIgnoreCase);
                    if (certificationStart > 0)
                    {
                        certificationStart = content.IndexOf(">", certificationStart, StringComparison.InvariantCultureIgnoreCase) + 2;
                        var certificationEnd = content.IndexOf("<", certificationStart, StringComparison.InvariantCultureIgnoreCase) - 1;
                        film.Rating = content.Substring(certificationStart, certificationEnd - certificationStart).Trim();
                    }
                    else
                    {
                        film.Rating = "?";
                        certificationStart = filmTitleEnd;
                    }

                    film.Data = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

                    var runningTimeStart = content.IndexOf("<h3>Running time:</h3>", certificationStart, StringComparison.InvariantCultureIgnoreCase);
                    if (runningTimeStart > 0)
                    {
                        runningTimeStart = content.IndexOf("<p>", runningTimeStart, StringComparison.InvariantCultureIgnoreCase) + 3;
                        var runningTimeEnd = content.IndexOf("</p>", runningTimeStart, StringComparison.InvariantCultureIgnoreCase);
                        film.Data["RunningTime"] = content.Substring(runningTimeStart, runningTimeEnd - runningTimeStart).Trim();
                    }
                    else
                    {
                        runningTimeStart = certificationStart;
                    }

                    var synopsisStart = content.IndexOf("<h3>Synopsis:</h3>", runningTimeStart, StringComparison.InvariantCultureIgnoreCase);
                    if (synopsisStart > 0)
                    {
                        synopsisStart = content.IndexOf("<p>", synopsisStart, StringComparison.InvariantCultureIgnoreCase) + 3;
                        var synopsisEnd = content.IndexOf("</p>", synopsisStart, StringComparison.InvariantCultureIgnoreCase);
                        film.Data["Synopsis"] = content.Substring(synopsisStart, synopsisEnd - synopsisStart).Trim();
                    }
                    else
                    {
                        synopsisStart = runningTimeStart;
                    }

                    var daysSubstring = content.Substring(synopsisStart);
                    var days = _daysParser.Parse(daysSubstring);
                    if (days != null && days.Any())
                    {
                        film.Days = new List<Day>(days);
                    }
                    else
                    {
                        film = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Fail(ex.ToString());
                    film = null;
                }

                return film;
            }
        }

        private class DaysParser
        {
            private readonly DateParser _dateParser = new DateParser();

            public IList<Day> Parse(string content)
            {
                IList<Day> days = new List<Day>();

                try
                {
                    var daySubStrings = content.Split(new[] { "<div class=\"row day\">" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var daySubString in daySubStrings)
                    {
                        var dayStart = daySubString.IndexOf("<h3>");
                        if (dayStart < 0)
                        {
                            continue;
                        }

                        dayStart = dayStart + 4;

                        var dayEnd = daySubString.IndexOf("</h3>", dayStart, StringComparison.InvariantCultureIgnoreCase);
                        if (dayEnd < 0)
                        {
                            continue;
                        }

                        var dateString = daySubString.Substring(dayStart, dayEnd - dayStart);

                        var timesSubStringStart = daySubString.IndexOf("<ol", dayEnd, StringComparison.InvariantCultureIgnoreCase);
                        if (timesSubStringStart < 0)
                        {
                            continue;
                        }

                        var timesSubString = daySubString.Substring(timesSubStringStart);

                        var showings = new List<Show>();
                        var date = _dateParser.GetDate(dateString);

                        var timeSubStrings = timesSubString.Split(new[] { "<li>" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var timeSubString in timeSubStrings)
                        {
                            var timeStart = timeSubString.IndexOf(">", StringComparison.InvariantCultureIgnoreCase) + 1;
                            var timeEnd = timeSubString.IndexOf("<", timeStart, StringComparison.InvariantCultureIgnoreCase);
                            if (timeEnd < 0)
                            {
                                continue;
                            }

                            var time = timeSubString.Substring(timeStart, timeEnd - timeStart);
                            var timeSpan = TimeSpan.Parse(time);

                            var showing = new Show
                            {
                                Time = date.Add(timeSpan)
                            };

                            var remainingText = timeSubString.Substring(timeEnd);
                            showing.Is2D = remainingText.Contains("icon-service-2d");
                            showing.Is3D = remainingText.Contains("icon-service-3d");
                            showing.DBox = remainingText.Contains("icon-service-dx");
                            showing.Superscreen = remainingText.Contains("icon-service-sr");
                            showing.Is4Dx = remainingText.Contains("icon-service-4dx");
                            showing.Vip = remainingText.Contains("icon-service-vp");
                            showing.AudioDescribed = remainingText.Contains("icon-service-ad");
                            showing.Imax = remainingText.Contains("icon-service-ix");

                            showings.Add(showing);
                        }

                        if (showings.Any())
                        {
                            var day = new Day
                            {
                                Date = date,
                                Shows = showings
                            };

                            days.Add(day);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Fail(ex.ToString());
                    days = null;
                }

                return days;
            }
        }

        private class DateParser
        {
            private readonly CultureInfo _britishCulture = new CultureInfo("en-GB");

            public DateTime GetDate(string input)
            {
                DateTime day;

                if (Parse(input, "dddd d MMM", out day))
                {
                    return day;
                }

                var nextYearInput = input + " " + (DateTime.UtcNow.Year + 1);
                if (Parse(nextYearInput, "dddd d MMM yyyy", out day))
                {
                    return day;
                }

                var message = string.Format("Invalid date: {0}", input);
                throw new FormatException(message);
            }

            private bool Parse(string input, string format, out DateTime date)
            {
                var processedInput = input.Replace("st ", " ").Replace("nd ", " ").Replace("rd ", " ").Replace("th ", " ");
                return DateTime.TryParseExact(processedInput, format, _britishCulture, DateTimeStyles.AllowWhiteSpaces, out date);
            }
        }
    }
}