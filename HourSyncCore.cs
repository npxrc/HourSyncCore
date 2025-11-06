#pragma warning disable IDE0007 // Use implicit type

// HourSyncCore.cs
//
// Push command since I forget every time:
// nuget push bin\publish\hoursynccore.<version>.nupkg -s https://api.nuget.org/v3/index.json

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace HourSyncCoreLib;

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

    /// <summary>
    /// Whether the process was successful or not.
    /// </summary>
    public bool Success
    {
        get; init;
    }
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
public record EHourRequest
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
}

/// <summary>
/// Represents accepted, denied, pending, and returned requests.
/// </summary>
public record EHourRequestsList
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

// ------------------ //
// -- Get a Request-- //
// ------------------ //

/// <summary>
/// A fetched eHour request's body (the content typed), worth (how many eHours), date submitted, list of images, and comments.
/// </summary>
public record FetchedEHourRequest
{
    /// <summary>
    /// From <see cref="EHourRequest.Value"/>:
    /// The value attribute of the eHour request button, formatted as:
    /// {StudentId}|{Two-letter academy abbreviation}|{Submission date}.
    /// This is used to uniquely identify and retrieve a specific eHour request.
    /// </summary>
    public required string Value
    {
        get; init;
    }

    public required DateTime Date
    {
        get; init;
    }

    /// <summary>
    /// The request's body.
    /// </summary>
    public required string Body
    {
        get; init;
    }

    public required string RequestedHours
    {
        get; init;
    }

    /// <summary>
    /// Images from the request.
    /// </summary>
    public List<string>? Images
    {
        get; init;
    }

    /// <summary>
    /// The comments box from an eHour request.
    /// </summary>
    public string? Comments
    {
        get; init;
    }

    /// <summary>
    /// Whether or not the request was a success.
    /// </summary>
    public required bool Success
    {
        get; init;
    }

    /// <summary>
    /// The error encountered, if any
    /// </summary>
    public string Error
    {
        get; init;
    }

    /// <summary>
    /// True if the client is still logged in. Useful for checking why Success is false.
    /// </summary>
    public required bool LoggedIn
    {
        get; init;
    }
}

public class UpdatedRequest
{
    /// <summary>
    /// The request ID.
    /// </summary>
    public required string Value
    {
        get; init;
    }
    /// <summary>
    /// The new content.
    /// </summary>
    public required string NewContent
    {
        get; init;
    }
    /// <summary>
    /// The request state. While you can technically lie about this, it's in your best interest to store the state from <see cref="ParseRequests(string)"/>.
    /// </summary>
    public required Status State
    {
        get; init;
    }
}

public class PostResponse
{
    /// <summary>
    /// Whether or not the request was a success.
    /// </summary>
    public required bool Success
    {
        get; init;
    }

    /// <summary>
    /// The error encountered, if any
    /// </summary>
    public string? Error
    {
        get; init;
    }

    /// <summary>
    /// True if the client is still logged in. Useful for checking why Success is false.
    /// </summary>
    public required bool LoggedIn
    {
        get; init;
    }

    /// <summary>
    /// The HTML response after a POST request, useful for refreshing home items.
    /// </summary>
    public string? HtmlResponse
    {
        get; init;
    }
}


/// <summary>
/// A record to contain a student on the leaderboard
/// </summary>
public record LeaderboardStudent
{
    /// <summary>
    /// The name of the student
    /// </summary>
    public required string Name
    {
        get; init;
    }

    /// <summary>
    /// How many hours the student has
    /// </summary>
    public required string Hours
    {
        get; init;
    }

    /// <summary>
    /// Which place the student is in compared to other students
    /// </summary>
    public required int Rank
    {
        get; init;
    }
}

/// <summary>
/// The result of fetching the leaderboard.
/// </summary>
public record LeaderboardData
{
    /// <summary>
    /// A list of student data. <see cref="LeaderboardStudent"/>
    /// </summary>
    public List<LeaderboardStudent>? StudentData
    {
        get; init;
    }

    /// <summary>
    /// Whether or not the request was a success.
    /// </summary>
    public bool Success
    {
        get; init;
    }

    /// <summary>
    /// The error encountered, if any
    /// </summary>
    public string? Error
    {
        get; init;
    }

    /// <summary>
    /// True if the client is still logged in. Useful for checking why Success is false.
    /// </summary>
    public bool LoggedIn
    {
        get; init;
    }
}

public static partial class HourSyncCore
{
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
        var startTime = DateTime.Now;
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentNullException(nameof(username));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentNullException(nameof(password));
        }
        if (!UsernameRegex().IsMatch(username))
        {
            throw new ArgumentException(
                $"The {nameof(username)} is not in the correct format. Expected format: 123abc45 (not an email).",
                nameof(username));
        }

        if (password.Length < 5)
        {
            throw new ArgumentException(
                $"{nameof(password)} does not meet the minimum length requirement of 5 characters.",
                nameof(password));
        }

        Log("Vars passed checks, setting up client");

        CookieContainer cookieContainer = new();
        using HttpClientHandler handler = new()
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = true,
        };

        using HttpClient client = new(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:142.0) Gecko/20100101 Firefox/142.0");

        Log("Client set up");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "uName", username },
            { "uPass", password }
        });

        HttpResponseMessage response;

        Log("Posting");
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

        Log("Posted");

        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Log("Unsuccessful");
            return new LoginResult
            {
                Error = $"Server returned status code {(int)response.StatusCode}."
            };
        }

        var cleanedResponse = responseString.Replace("\n", "");

        if (cleanedResponse.Contains("<h2>Welcome to your"))
        {
            Log("Correct credentials, returning");
            Uri baseUri = new("https://academyendorsement.olatheschools.com/");
            var phpSessionId = cookieContainer.GetCookies(baseUri)["PHPSESSID"]?.Value;

            var nameOfAcademy = ExtractBetween(cleanedResponse, "<h2>Welcome to your ", " Endorsement");
            var nameOfPerson = ExtractBetween(cleanedResponse, "Tracking, ", "</h2>");

            Log($"Done in {(DateTime.Now - startTime).TotalMilliseconds} ms");
            return new LoginResult
            {
                PhpSessionId = phpSessionId,
                LoginHtmlResponse = responseString,
                StudentName = nameOfPerson,
                StudentAcademy = nameOfAcademy,
                Error = null,
                Success = true
            };
        }
        else if (cleanedResponse.Contains("<p>Incorrect Username or Password</p>"))
        {
            Log("Incorrect credentials");
            Log($"Done in {(DateTime.Now - startTime).TotalMilliseconds} ms");
            return new LoginResult
            {
                Error = "Incorrect credentials",
                Success = false
            };
        }
        else
        {
            Log("An error occurred.");
            Log($"Done in {(DateTime.Now - startTime).TotalMilliseconds} ms");
            return new LoginResult
            {
                LoginHtmlResponse = cleanedResponse,
                Error = "Unknown error"
            };
        }
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
        var startTime = DateTime.Now;
        if (string.IsNullOrWhiteSpace(phpSessionId))
        {
            throw new ArgumentNullException(nameof(phpSessionId));
        }
        Log("Session ID: " + phpSessionId);
        Log("Vars passed checks, setting client up");

        var (client, _, __) = CreateHttpClient(phpSessionId);

        Log("Getting requests");
        var response = await client.GetAsync("https://academyendorsement.olatheschools.com/Student/studentEHours.php");
        Log("Got requests");
        var responseString = await response.Content.ReadAsStringAsync();

        Log($"Done in {(DateTime.Now - startTime).TotalMilliseconds} ms");
        return responseString;
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
                        if (tds?.Count >= 3 && currentSection != null)
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

    /// <summary>
    /// Gets a specified eHour request
    /// </summary>
    /// <param name="phpSessionId">Your session ID</param>
    /// <param name="requestId">The ID of the request you want to fetch</param>
    /// <param name="fixTextEncoding">Whether you want to fix text encoding mismatches (for dynamic quotations, etc.)</param>
    /// <returns>A <see cref="FetchedEHourRequest"/> with the specified request body, images, and comments.</returns>
    /// <exception cref="ArgumentNullException">If the session id or request id are null or empty</exception>
    /// <exception cref="Exception">If 'whitetext' nodes could not be found</exception>
    public static async Task<FetchedEHourRequest> GetRequest(string phpSessionId, string requestId, bool fixTextEncoding = true)
    {
        //boilerplate setup
        if (string.IsNullOrWhiteSpace(phpSessionId))
        {
            throw new ArgumentNullException(nameof(phpSessionId));
        }
        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentNullException(nameof(requestId));
        }

        var (client, _, __) = CreateHttpClient(phpSessionId);

        //get request
        var values = new Dictionary<string, string> { { "ehours_request_descr", requestId } };
        var content = new FormUrlEncodedContent(values);

        var response = await client.PostAsync(
            "https://academyendorsement.olatheschools.com/Student/eHourDescription.php",
            content
        );

        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        var charset = response.Content.Headers.ContentType?.CharSet;

        Encoding encoding = GetEncodingFromCharset(charset);
        var responseString = encoding.GetString(responseBytes);

        if (!responseString.Contains("Requested Number of Hours"))
        {
            return new FetchedEHourRequest() { Body = null, Date = DateTime.Now, LoggedIn = false, RequestedHours = null, Value = null, Success = false, Error = "Not logged in." };
        }

        HtmlAgilityPack.HtmlDocument doc = new();
        doc.LoadHtml(responseString);
        var whiteTextNodes = doc.DocumentNode.SelectNodes("//*[@class='whitetext']");
        if (whiteTextNodes?.Count >= 2)
        {
            string reqdHrs = HttpUtility.HtmlDecode(whiteTextNodes[0].InnerText);
            reqdHrs = reqdHrs.Split(':')[1].Split(' ')[1];
            string dateSubmitted = HttpUtility.HtmlDecode(whiteTextNodes[1].InnerText);

            string dateFinalText = string.Join(":", dateSubmitted.Split(':').Skip(1));
            DateTime dateSubtd = DateTime.ParseExact(
                dateFinalText.TrimStart(),
                ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.fff"],
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None
            );

            string? desc = HttpUtility.HtmlDecode(
                doc.DocumentNode.SelectSingleNode("//textarea[@id='description']")?.InnerText
            );
            string? comments = HttpUtility.HtmlDecode(
                doc.DocumentNode.SelectSingleNode("//textarea[@id='comments']")?.InnerText
            );

            if (fixTextEncoding)
            {
                desc = FixMojibake(desc!);
                comments = FixMojibake(comments!);
            }

            List<string> imagePaths = ParseAndFindImages(doc);

            return new FetchedEHourRequest()
            {
                Value = requestId,
                Body = desc,
                RequestedHours = reqdHrs,
                Comments = comments,
                Images = imagePaths,
                Date = dateSubtd,
                Success = true,
                LoggedIn = true,
            };
        }
        else
        {
            throw new Exception("Could not find 'whitetext' nodes.");
        }
    }

    /// <summary>
    /// Edits an existing EHour Request. Must be a pending request.
    /// </summary>
    /// <param name="phpSessionId">Your session ID.</param>
    /// <param name="request">The request to update.</param>
    /// <returns>A <see cref="PostResponse"/> which has Success, Error, and LoggedIn properties.</returns>
    public static async Task<PostResponse> EditRequest(string phpSessionId, UpdatedRequest request)
    {
        if (request.State != Status.Pending)
        {
            throw new InvalidOperationException("The request must be pending for you to edit it.");
        }
        var (client, _, __) = CreateHttpClient(phpSessionId);
        var values = new Dictionary<string, string>
        {
            { "description", request.NewContent },
            { "comments", "" },
            { "update", request.Value }
        };

        using var content = new FormUrlEncodedContent(values);

        try
        {
            var response = await client.PostAsync("https://academyendorsement.olatheschools.com/editRequest.php", content);
            var responseString = await response.Content.ReadAsStringAsync();
            if (responseString.Contains("See your current eHours"))
            {
                return new PostResponse() { LoggedIn = true, Success = true, HtmlResponse = responseString };
            }
            else
            {
                return new PostResponse() { LoggedIn = false, Success = false, Error = "Not logged in.", HtmlResponse = responseString };
            }
        }
        catch (Exception e)
        {
            return new PostResponse() { LoggedIn = false, Success = false, Error = e.Message };
        }
    }

    /// <summary>
    /// Fetches and returns the leaderboard, parsed.
    /// </summary>
    /// <param name="phpSessionId">Your session ID</param>
    /// <returns><see cref="LeaderboardData"/></returns>
    public static async Task<LeaderboardData> GetLeaderboard(string phpSessionId)
    {
        try
        {
            var (client, _, __) = CreateHttpClient(phpSessionId);
            Log("Client has been set up, getting");
            var response = await client.GetAsync("https://academyendorsement.olatheschools.com/Student/leaderBoard.php");
            var responseString = await response.Content.ReadAsStringAsync();
            Log("Got");
            Log(responseString);

            var doc = new HtmlDocument();
            doc.LoadHtml(responseString);

            var titleNode = doc.DocumentNode.SelectSingleNode("//h1[contains(text(), 'EHour')]");

            if (titleNode != null)
            {
                var rows = doc.DocumentNode.SelectNodes("//table//tr[td]");
                var result = rows
                            .Select(r => r.SelectNodes("td").ToList())
                            .Where(cells => !string.IsNullOrWhiteSpace(cells[1].InnerText))
                            .Select((cells, index) => new LeaderboardStudent
                            {
                                Rank = index + 1,
                                Name = cells[1].InnerText.Trim(),
                                Hours = double.TryParse(cells[2].InnerText.Trim(), out double hoursVal) ? hoursVal.ToString("0.##") : cells[2].InnerText.Trim()
                            })
                            .ToList();


                return new LeaderboardData { StudentData = result, LoggedIn = true, Success = true };
            }
            else
            {
                return new LeaderboardData { StudentData = null, LoggedIn = false, Success = false };
            }

        }
        catch (Exception ex)
        {
            return new LeaderboardData() { StudentData = null, LoggedIn = false, Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Check if your session ID is still active by testing it against the home page.
    /// </summary>
    /// <param name="phpSessionId">The session ID to test</param>
    /// <returns>A bool of true if active, and false if inactive.</returns>
    public static async Task<bool> IsSessionActive(string phpSessionId)
    {
        var (client, _, __) = CreateHttpClient(phpSessionId);
        var response = client.GetAsync("https://academyendorsement.olatheschools.com/Student/studentHome.php");
        var content = await response.Result.Content.ReadAsStringAsync();
        return content.Contains("Welcome to your");
    }

    //utils
    [GeneratedRegex(@"^\d{3}[a-zA-Z]{3}(0[1-9]|[12][0-9]|3[01])$")]
    public static partial Regex UsernameRegex();

    public static (HttpClient Client, HttpClientHandler Handler, CookieContainer Cookies)
    CreateHttpClient(string phpSessionId)
    {
        var cookieContainer = new CookieContainer();

        var target = new Uri("https://academyendorsement.olatheschools.com/");
        cookieContainer.Add(
            new Cookie("PHPSESSID", phpSessionId) { Domain = target.Host }
        );

        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = true,
        };

        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:142.0) Gecko/20100101 Firefox/142.0"
        );

        return (client, handler, cookieContainer);
    }
    public static void Log(string toLog)
    {
        string logFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HourSyncCore",
            "log.txt"
        );
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            string timestamp = DateTime.Now.ToString("MM/dd/yy HH:mm:ss:fff");
            File.AppendAllText(logFilePath, $"\r\n{timestamp} - {toLog}");

            System.Diagnostics.Trace.WriteLine($"{timestamp} - ${toLog}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static Encoding GetEncodingFromCharset(string? charset)
    {
        return charset switch
        {
            "iso-8859-1" or "latin1" => Encoding.GetEncoding("ISO-8859-1"),
            "windows-1252" => Encoding.GetEncoding("windows-1252"),
            _ => Encoding.UTF8
        };
    }

    public static string FixMojibake(string text)
    {
        return text.Replace("â??", "'")
                   .Replace("â€™", "'")
                   .Replace("â€œ", "\"")
                   .Replace("â€\u009d", "\"")
                   .Replace("â€\"", "–")
                   .Replace("â€”", "—");
    }

    public static List<string> ParseAndFindImages(HtmlDocument doc)
    {
        if (doc == null)
        {
            return [];
        }
        // Create a base URL for relative paths
        const string baseUrl = "https://academyendorsement.olatheschools.com/";

        // Select all image nodes
        var imgNodes = doc.DocumentNode.SelectNodes("//img");
        if (imgNodes != null)
        {
            List<string> imagePaths = [];
            foreach (var imgNode in imgNodes)
            {
                var src = imgNode.GetAttributeValue("src", string.Empty);

                // If src contains "../", it needs to be fixed
                if (src.StartsWith("../"))
                {
                    src = string.Concat(baseUrl, src.AsSpan(3));
                }

                imagePaths.Add(src);
            }
            return imagePaths;
        }
        return [];
    }

    /// <summary>
    /// Extracts the substring between two markers, or null if not found.
    /// </summary>
    public static string? ExtractBetween(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        if (startIndex == -1)
        {
            return null;
        }

        startIndex += start.Length;

        var endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        return endIndex == -1 ? null : source[startIndex..endIndex].Trim();
    }

}