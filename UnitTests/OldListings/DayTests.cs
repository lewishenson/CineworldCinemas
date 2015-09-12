﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentCineworld.OldListings;
using Xunit;

namespace FluentCineworld.UnitTests.OldListings
{
    [Trait("Category", "UnitTest")]
    public class DayTests
    {
        [Fact]
        public void GivenThereAreNoDays_WhenMerging_ThenNullIsReturned()
        {
            var mergeResult = Day.Merge(Enumerable.Empty<Day>());

            mergeResult.Should().BeNull();
        }

        [Fact]
        public void GivenThereIsOneDay_WhenMerging_ThenThatDayIsReturned()
        {
            var day = new Day();

            var mergeResult = Day.Merge(new[] { day });

            mergeResult.Should().Be(day);
        }

        [Fact]
        public void GivenThereAreMultipleDaysWithDifferentDates_WhenMerging_ThenAnExceptionShouldBeThrown()
        {
            var day1 = new Day { Date = new DateTime(2015, 1, 1) };
            var day2 = new Day { Date = new DateTime(2015, 1, 2) };

            Action mergeAction = () => Day.Merge(new[] { day1, day2 });

            mergeAction.ShouldThrow<ArgumentException>()
                       .WithMessage("All days must have the same date.");
        }

        [Fact]
        public void GivenThereAreMultipleDays_WhenMerging_ThenTheResultShouldContainAllShows()
        {
            var day1 = new Day
            {
                Date = new DateTime(2015, 1, 1),
                Shows = new List<Show>
                        {
                            new Show { Time = new DateTime(2015, 1, 1, 12, 0, 0) }
                        }
            };

            var day2 = new Day
            {
                Date = new DateTime(2015, 1, 1),
                Shows = new List<Show>
                        {
                            new Show { Time = new DateTime(2015, 1, 1, 13, 0, 0) }
                        }
            };

            var mergeResult = Day.Merge(new[] { day1, day2 });

            var expectedResult = new Day
            {
                Date = new DateTime(2015, 1, 1),
                Shows = new List<Show>
                        {
                            new Show { Time = new DateTime(2015, 1, 1, 12, 0, 0) },
                            new Show { Time = new DateTime(2015, 1, 1, 13, 0, 0) }
                        }
            };

            mergeResult.ShouldBeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GivenThereAreMultipleDays_WhenMerging_ThenTheResultShouldHaveSortedShows()
        {
            var day1 = new Day
            {
                Date = new DateTime(2015, 1, 1),
                Shows = new List<Show>
                        {
                            new Show { Time = new DateTime(2015, 1, 1, 12, 0, 0) }
                        }
            };

            var day2 = new Day
            {
                Date = new DateTime(2015, 1, 1),
                Shows = new List<Show>
                        {
                            new Show { Time = new DateTime(2015, 1, 1, 0, 0, 0) }
                        }
            };

            var mergeResult = Day.Merge(new[] { day1, day2 });

            var mergedShows = mergeResult.Shows.Select(d => d.Time).ToList();
            mergedShows.Should().BeInAscendingOrder();
        }
    }
}