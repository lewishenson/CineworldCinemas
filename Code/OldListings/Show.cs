﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluentCineworld.OldListings
{
    public class Show
    {
        public DateTime Time { get; set; }

        public bool Is2D { get; set; }

        public bool Is3D { get; set; }

        public bool DBox { get; set; }

        public bool Vip { get; set; }

        public bool Imax { get; set; }

        public bool Superscreen { get; set; }

        public bool Is4Dx { get; set; }

        public bool AudioDescribed { get; set; }

        public bool Subtitled { get; set; }

        public string DisplayText
        {
            get
            {
                return ToString();
            }
        }

        public static Show Merge(IEnumerable<Show> shows)
        {
            if (!shows.Any())
            {
                return null;
            }

            if (shows.Count() == 1)
            {
                return shows.Single();
            }

            var firstShow = shows.First();
            if (shows.Any(s => s.Time != firstShow.Time))
            {
                throw new ArgumentException("All shows must have the same time.");
            }

            var mergeResult = new Show { Time = firstShow.Time };

            foreach (var show in shows)
            {
                mergeResult.Is2D |= show.Is2D;
                mergeResult.Is3D |= show.Is3D;
                mergeResult.DBox |= show.DBox;
                mergeResult.Vip |= show.Vip;
                mergeResult.Imax |= show.Imax;
                mergeResult.AudioDescribed |= show.AudioDescribed;
                mergeResult.Subtitled |= show.Subtitled;
                mergeResult.Is4Dx |= show.Is4Dx;
                mergeResult.Superscreen |= show.Superscreen;
            }

            return mergeResult;
        }

        public override string ToString()
        {
            var outputBuilder = new StringBuilder();

            AppendWithCommaIfPropertyEqualToTrue(Is2D, outputBuilder, "2D");
            AppendWithCommaIfPropertyEqualToTrue(Is3D, outputBuilder, "3D");
            AppendWithCommaIfPropertyEqualToTrue(DBox, outputBuilder, "DBOX");
            AppendWithCommaIfPropertyEqualToTrue(Vip, outputBuilder, "VIP");
            AppendWithCommaIfPropertyEqualToTrue(Imax, outputBuilder, "IMAX");
            AppendWithCommaIfPropertyEqualToTrue(Superscreen, outputBuilder, "Superscreen");
            AppendWithCommaIfPropertyEqualToTrue(Is4Dx, outputBuilder, "4DX");
            AppendWithCommaIfPropertyEqualToTrue(AudioDescribed, outputBuilder, "Audio Described");
            AppendWithCommaIfPropertyEqualToTrue(Subtitled, outputBuilder, "Subtitled");

            if (outputBuilder.Length == 0)
            {
                return Time.ToString("HH:mm");
            }

            outputBuilder.Insert(0, " (");
            outputBuilder.Insert(0, Time.ToString("HH:mm"));
            outputBuilder.Append(")");

            return outputBuilder.ToString();
        }

        private void AppendWithCommaIfPropertyEqualToTrue(bool propertyValue, StringBuilder stringBuilder, string value)
        {
            if (!propertyValue)
            {
                return;
            }

            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append(", ");
            }

            stringBuilder.Append(value);
        }
    }
}