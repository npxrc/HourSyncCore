#pragma warning disable IDE0007 // Use implicit type

using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace HourSyncCoreLib;

public static class HourSyncCore
{
    // ----------- //
    // -- Login -- //
    // ----------- //

    /// <summary>
    /// Contains the results of a login attempt.
    /// </summary>
    public record LoginResult
    {
        /// <summary>
        /// The PHP session ID returned by the server.
        /// </summary>
        public string? PhpSessionId
        {
            get; init;
        }

        /// <summary>
        /// The full HTML response from the IMMEDIATE home page (not the list of eHours).
        /// </summary>
        public string? LoginHtmlResponse
        {
            get; init;
        }

        /// <summary>
        /// The user's full name.
        /// </summary>
        public string? StudentName
        {
            get; init;
        }

        /// <summary>
        /// The academy the user is in.
        /// </summary>
        public string? StudentAcademy
        {
            get; init;
        }

        /// <summary>
        /// The error message if login failed; otherwise null.
        /// </summary>
        public string? Error
        {
            get; init;
        }
    }

    /// <summary>
    /// Logs a user into the eHours portal.
    /// </summary>
    /// <param name="username">The user's username in the format 123abc45 (not an email).</param>
    /// <param name="password">The user's password, at least 5 characters long.</param>
    /// <returns>
    /// A <see cref="LoginResult"/> containing the session ID, HTML response, student name, and academy if successful; otherwise an error message.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="username"/> or <paramref name="password"/> is null or empty.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="username"/> is not in the required format or 
    /// <paramref name="password"/> is too short.
    /// </exception>
    public static async Task<LoginResult> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentNullException(nameof(password));

        var pattern = @"^\d{3}[a-zA-Z]{3}(0[1-9]|[12][0-9]|3[01])$";
        if (!Regex.IsMatch(username, pattern))
            throw new ArgumentException(
                $"The {nameof(username)} is not in the correct format. Expected format: 123abc45 (not an email).",
                nameof(username));

        if (password.Length < 5)
            throw new ArgumentException(
                $"{nameof(password)} does not meet the minimum length requirement of 5 characters.",
                nameof(password));

        CookieContainer cookieContainer = new();
        using HttpClientHandler handler = new()
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = true,
        };

        using HttpClient client = new(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "uName", username },
            { "uPass", password }
        });

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(
                "https://academyendorsement.olatheschools.com/loginuserstudent.php",
                content);
        }
        catch (HttpRequestException ex)
        {
            return new LoginResult
            {
                Error = $"Network error: {ex.Message}"
            };
        }

        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new LoginResult
            {
                Error = $"Server returned status code {(int)response.StatusCode}."
            };
        }

        var cleanedResponse = responseString.Replace("\n", "");

        if (cleanedResponse.Contains("<h2>Welcome to your"))
        {
            Uri baseUri = new("https://academyendorsement.olatheschools.com/");
            var phpSessionId = cookieContainer.GetCookies(baseUri)["PHPSESSID"]?.Value;

            var nameOfAcademy = ExtractBetween(cleanedResponse, "<h2>Welcome to your ", " Endorsement");
            var nameOfPerson = ExtractBetween(cleanedResponse, "Tracking, ", "</h2>");

            return new LoginResult
            {
                PhpSessionId = phpSessionId,
                LoginHtmlResponse = responseString,
                StudentName = nameOfPerson,
                StudentAcademy = nameOfAcademy,
                Error = null
            };
        }
        else if (cleanedResponse.Contains("<p>Incorrect Username or Password</p>"))
        {
            return new LoginResult
            {
                Error = "Incorrect credentials"
            };
        }
        else
        {
            return new LoginResult
            {
                LoginHtmlResponse = cleanedResponse,
                Error = "Unknown error"
            };
        }
    }

    /// <summary>
    /// Extracts the substring between two markers, or null if not found.
    /// </summary>
    private static string? ExtractBetween(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        if (startIndex == -1) return null;
        startIndex += start.Length;

        var endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        if (endIndex == -1) return null;

        return source.Substring(startIndex, endIndex - startIndex).Trim();
    }

    // -------------- //
    // -- Get Home -- //
    // -------------- //

    /// <summary>
    /// Fetches and returns a string with the home page as HTML, unparsed.
    /// </summary>
    /// <param name="phpSessionId">The PHP session ID returned by the server.</param>
    /// <returns>A string with the home page as HTML, unparsed.</returns>
    public static async Task<string> GetRequestsPage(string phpSessionId)
    {
        if (string.IsNullOrWhiteSpace(phpSessionId))
        {
            throw new ArgumentNullException(nameof(phpSessionId));
        }

        CookieContainer cookieContainer = new();

        // fix courtesy of https://stackoverflow.com/questions/18667931/httpwebrequest-add-cookie-to-cookiecontainer-argumentexception-parameternam
        var target = new Uri("https://academyendorsement.olatheschools.com/");
        cookieContainer.Add(new Cookie("PHPSESSID", phpSessionId) { Domain = target.Host });
        using HttpClientHandler handler = new()
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = true,
        };

        using HttpClient client = new(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

        var response = await client.GetAsync("https://academyendorsement.olatheschools.com/Student/studentEHours.php");
        var responseString = await response.Content.ReadAsStringAsync();

        return responseString;
    }

    // ---------------- //
    // -- Parse Home -- //
    // ---------------- //

    public enum Status
    {
        Accepted,
        Denied,
        Pending,
        Returned
    }
    /// <summary>
    /// Represents a parsed eHour request.
    /// </summary>
    public class EHourRequest
    {
        /// <summary>
        /// The value attribute of the eHour request button, formatted as:
        /// {StudentId}|{Two-letter academy abbreviation}|{Submission date}.
        /// This is used to uniquely identify and retrieve a specific eHour request.
        /// </summary>
        public required string Value
        {
            get; init;
        }

        /// <summary>
        /// The title or brief description of the eHour request.
        /// </summary>
        public required string Description
        {
            get; init;
        }

        /// <summary>
        /// The total number of hours requested.
        /// </summary>
        public required string Hours
        {
            get; init;
        }

        /// <summary>
        /// The date and time the request was submitted.
        /// </summary>
        public required string Date
        {
            get; init;
        }

        /// <summary>
        /// The status of the request. Either Accepted, Denied, Pending, or Returned.
        /// </summary>
        public Status State
        {
            get; init;
        }

        /// <summary>
        /// The request's body.
        /// </summary>
        public string Body
        {
            get; init;
        }
    }
    /// <summary>
    /// Represents accepted, denied, pending, and returned requests.
    /// </summary>
    public class EHourRequestsList
    {
        /// <summary>
        /// Accepted requests
        /// </summary>
        public required List<EHourRequest> Accepted
        {
            get; set;
        }
        /// <summary>
        /// Denied requests
        /// </summary>
        public required List<EHourRequest> Denied
        {
            get; set;
        }
        /// <summary>
        /// Pending requests
        /// </summary>
        public required List<EHourRequest> Pending
        {
            get; set;
        }
        /// <summary>
        /// Returned requests
        /// </summary>
        public required List<EHourRequest> Returned
        {
            get; set;
        }
    }
    /// <summary>
    /// Parses eHour Requests based on status (accepted, denied, pending, returned).
    /// </summary>
    /// <param name="homePageHtml">The HTML home page string</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="Exception"></exception>
    public static EHourRequestsList ParseRequests(string homePageHtml)
    {
        if (string.IsNullOrWhiteSpace(homePageHtml))
        {
            throw new ArgumentNullException(nameof(homePageHtml));
        }
        HtmlAgilityPack.HtmlDocument doc = new();
        doc.LoadHtml(homePageHtml);
        try
        {
            // Select the table directly by its ID
            var table = doc.DocumentNode.SelectSingleNode("//table[@id='eHourRequests']");
            if (table != null)
            {
                // Get the tbody from the table. If not found, look at the table itself for rows.
                var tableBody = table.SelectSingleNode("tbody");
                var containerNode = tableBody ?? table; // Use tbody if it exists, otherwise use the table node itself.

                // Section markers
                EHourRequestsList Requests = new()
                {
                    Accepted = [],
                    Denied = [],
                    Pending = [],
                    Returned = []
                };

                var sectionMap = new Dictionary<string, List<EHourRequest>>
                {
                    { "Returned_Hours", Requests.Returned },
                    { "Pending_Hours", Requests.Pending },
                    { "Accepted_Hours", Requests.Accepted },
                    { "Denied_Hours", Requests.Denied }
                };


#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                string currentSection = null;
#pragma warning restore CS8600
                var rows = containerNode.SelectNodes(".//tr");
                if (rows != null)
                {
                    // The first row is the header, which is already present
                    // in the `<tbody>` in the example HTML, so we don't need to skip it.
                    // We'll just loop through all rows and handle section headers as before.
                    foreach (var row in rows)
                    {
                        // Check if this row is a section header
                        var th = row.SelectSingleNode("./th[@id]");
                        if (th != null)
                        {
                            var sectionId = th.GetAttributeValue("id", null!);
                            if (sectionMap.ContainsKey(sectionId))
                            {
                                currentSection = sectionId;
                            }
                            continue;
                        }

                        // Only process rows with <td> (request entries)
                        var tds = row.SelectNodes("./td");
                        if (tds != null && tds.Count >= 3 && currentSection != null)
                        {
                            var buttonNode = tds[0].SelectSingleNode(".//button[@name='ehours_request_descr']");
                            if (buttonNode != null)
                            {
                                var value = buttonNode.GetAttributeValue("value", string.Empty);
                                var description = HttpUtility.HtmlDecode(buttonNode.InnerText.Trim());
                                var hours = tds[1].InnerText.Trim();
                                var date = tds[2].InnerText.Trim();

                                var sectionType = currentSection.Split("_")[0];

                                if (!Enum.TryParse<Status>(sectionType, true, out var state))
                                {
                                    state = Status.Pending; // default fallback
                                }


                                var request = new EHourRequest
                                {
                                    Value = value,
                                    Description = description,
                                    Hours = hours,
                                    Date = date,
                                    State = state
                                };

                                sectionMap[currentSection].Add(request);
                            }
                        }
                    }
                    return Requests;
                }
                else
                {
                    throw new ArgumentException(message: "Table found, but no rows were found", paramName: nameof(homePageHtml));
                }
            }
            else
            {
                throw new ArgumentException(message: "Could not find a table to parse eHour requests from", paramName: nameof(homePageHtml));
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}