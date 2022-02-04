using System.Collections.Generic;

namespace Jellyfin.Plugin.Reports.Api.Model
{

    /// <summary> A report group. </summary>
    public class ReportGroup
    {
        /// <summary>
        /// Initializes a new instance of the MediaBrowser.Api.Reports.ReportGroup class. </summary>
        /// <param name="rows"> The rows. </param>
        public ReportGroup(string name, List<ReportRow> rows)
        {
            Name = name;
            Rows = rows;
        }

        /// <summary>Gets the name.</summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>Gets the rows.</summary>
        /// <value>The rows.</value>
        public List<ReportRow> Rows { get; }

        /// <summary> Returns a string that represents the current object. </summary>
        /// <returns> A string that represents the current object. </returns>
        /// <seealso cref="Object.ToString()"/>
        public override string ToString() => Name;
    }
}
