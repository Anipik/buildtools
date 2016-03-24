﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Globalization;

namespace Microsoft.DotNet.Build.Tasks
{
    public sealed class GenerateCurrentVersion : Task
    {
        /// <summary>
        /// The passed in date that will be used to generate a version. (yyyy-MM-dd format)
        /// </summary>
        [Required]
        public string SeedDate { get; set; }

        /// <summary>
        /// Optional parameter that sets the Padding for the version number. Must be 5 or bigger.
        /// </summary>
        public int Padding { get; set; }

        /// <summary>
        /// If basing off of internal builds version format is not required, this optional parameter lets you pass in a comparison date.
        /// </summary>
        public string ComparisonDate { get; set; }

        /// <summary>
        /// The Major Version that will be produced given a SeedDate.
        /// </summary>
        [Output]
        public string GeneratedVersion { get; set; }

        private const string DateFormat = "yyyy-MM-dd";
        private const string LastModifiedTimeDateFormat = "yyyy-MM-dd HH:mm:ss.FFFFFFF";
        private CultureInfo enUS = new CultureInfo("en-US");

        public override bool Execute()
        {
            if (Padding == 0)
            {
                Padding = 5;
            }
            else if (Padding < 5)
            {
                Log.LogWarning("The specified Padding '{0}' has to be equal to or greater than 5. Using 5 as a default now.", Padding);
                Padding = 5;
            }
            DateTime date;
            GeneratedVersion = string.Empty;
            if (!(DateTime.TryParseExact(SeedDate, DateFormat, enUS, DateTimeStyles.AssumeLocal, out date)))
            {
                // Check if the timestamp matches the LastModifiedTimeDateFormat
                if (!(DateTime.TryParseExact(SeedDate, LastModifiedTimeDateFormat, enUS, DateTimeStyles.AssumeLocal, out date)))
                {
                    Log.LogError("The seed date '{0}' is not valid. Please specify a date in the short format.({1})", SeedDate, DateFormat);
                    return false;
                }
            }
            //Convert Date to UTC to converge
            date = date.ToUniversalTime();
            GeneratedVersion = GetCurrentVersionForDate(date, ComparisonDate);
            if (string.IsNullOrEmpty(GeneratedVersion))
            {
                Log.LogError("The date '{0}' is not valid. Please pass in a date after {1}.", SeedDate, ComparisonDate);
                return false;
            }
            return true;
        }

        public string GetCurrentVersionForDate(DateTime seedDate, string comparisonDate)
        {
            DateTime compareDate;
            if (string.IsNullOrEmpty(comparisonDate))
            {
                /*
                 * We need to ensure that our build numbers are higher that what we used to ship internal builds so this date
                 * will make that possible.
                 */
                compareDate = new DateTime(1996, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                bool isValidDate = DateTime.TryParseExact(comparisonDate, DateFormat, enUS, DateTimeStyles.AssumeLocal, out compareDate);
                if (!isValidDate)
                {
                    Log.LogError("The comparison date '{0}' is not valid. Please specify a date in the short format.({1})", comparisonDate, DateFormat);
                }
                //Convert to UTC to converge
                compareDate = compareDate.ToUniversalTime();
            }
            int months = (seedDate.Year - compareDate.Year) * 12 + seedDate.Month - compareDate.Month;
            if (months > 0) //only allow dates after comparedate
            {
                return string.Format("{0}{1}", months.ToString("D" + (Padding -2)), seedDate.Day.ToString("D2"));
            }
            return string.Empty;
        }

    }
}