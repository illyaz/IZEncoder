﻿#region Copyright (C) 2005-2017 Team MediaPortal

// Copyright (C) 2005-2017 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

namespace IZEncoder.Common.MediaInfo.Builder
{
    using System;

    /// <summary>
    ///     Describes base methods to build media stream with language
    /// </summary>
    /// <typeparam name="TStream">The type of the stream.</typeparam>
    internal abstract class LanguageMediaStreamBuilder<TStream> : MediaStreamBuilder<TStream>
        where TStream : LanguageMediaStream, new()
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LanguageMediaStreamBuilder{TStream}" /> class.
        /// </summary>
        /// <param name="info">The media info object.</param>
        /// <param name="number">The stream number.</param>
        /// <param name="position">The stream position.</param>
        protected LanguageMediaStreamBuilder(MediaInfo info, int number, int position)
            : base(info, number, position) { }

        /// <inheritdoc />
        public override TStream Build()
        {
            var result = base.Build();
            var language = Get("Language").ToLower();
            result.Language = LanguageHelper.GetLanguageByShortName(language);
            result.Default = Get("Default", (string s, out bool r) =>
            {
                if (bool.TryParse(s, out r))
                    return true;

                if (s.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                {
                    r = true;
                    return true;
                }

                if (s.Equals("No", StringComparison.OrdinalIgnoreCase))
                {
                    r = false;
                    return true;
                }

                return false;
            });
            result.Forced = Get("Forced", (string s, out bool r) =>
            {
                if (bool.TryParse(s, out r))
                    return true;

                if (s.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                {
                    r = true;
                    return true;
                }

                if (s.Equals("No", StringComparison.OrdinalIgnoreCase))
                {
                    r = false;
                    return true;
                }

                return false;
            });
            result.Lcid = LanguageHelper.GetLcidByShortName(language);
            return result;
        }
    }
}