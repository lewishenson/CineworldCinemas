﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentCineworld.Utilities;

namespace FluentCineworld.OldListings
{
    public class CineworldListings : ICineworldListings
    {
        private readonly Cinema _cinema;

        private readonly Filter _filter;

        private readonly IWebClient _webClient;

        private IScraper<IEnumerable<Film>> _scraper;

        internal CineworldListings(Cinema cinema)
        {
            _cinema = cinema;
            _filter = new Filter();
            _webClient = new WebClient();
            _scraper = new SyndicationListingsScraper(_webClient);
        }

        public ICineworldListings UsingSyndication(bool useSyndication)
        {
            if (useSyndication)
            {
                InitialiseScraper<SyndicationListingsScraper>();
            }
            else
            {
                InitialiseScraper<SiteListingsScraper>();
            }

            return this;
        }

        private void InitialiseScraper<TScraper>()
            where TScraper : IScraper<IEnumerable<Film>>
        {
            if (_scraper.GetType() != typeof(TScraper))
            {
                _scraper = (TScraper)Activator.CreateInstance(typeof(TScraper), _webClient);
            }
        }

        public ICineworldListings ForDayOfWeek(DayOfWeek dayOfWeek)
        {
            _filter.DayOfWeek(dayOfWeek);
            return this;
        }

        public ICineworldListings From(DateTime from)
        {
            _filter.From(from);
            return this;
        }

        public ICineworldListings To(DateTime to)
        {
            _filter.To(to);
            return this;
        }

        public IEnumerable<Film> Retrieve()
        {
            var movies = _scraper.Scrape(_cinema);

            movies = _filter.Apply(movies);
            movies = movies.OrderBy(f => f.Title);

            return movies;
        }
    }
}