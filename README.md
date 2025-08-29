# HourSyncCore
The elephant in the room: yes, 99% of the code overlaps with previous releases of HourSync. This was an easy way to trim some fat in the HourSync app and an easy way to allow others to use my work.

# What is this?
HourSyncCore is a way to interface with the Academy Endorsement (colloquially called the eHours portal) website by the Olathe School District.

It interfaces easily with the website and makes it stupid simple for you to make your own version of HourSync.

# Features
- Logging into the portal
- Getting the eHours page (aka the home page)
- Parsing the home page in an easy to manipulate format

Fetching specific requests, updating said requests, and creating requests will come in a future update.

# Usage
Really easy.

Until the package is on NuGet, simply download the latest version from the Releases tab, open your project, right click on "Dependencies" and click "Add Project Reference". Browse and select the DLL.

Add the following using statement to the top of each file you use HourSync in:
```c#
using Core = HourSyncCoreLib.HourSyncCore;
```

### Logging in
Quite straightforward:

```c#
var loginResult = await HourSyncCore.Login(username, password);
// No, you do not need to use Core.HourSyncCore.Login,
// simply HourSyncCore works as long as you've placed
// using Core = HourSyncCoreLib.HourSyncCore at the top

if (!string.IsNullOrEmpty(loginResult.Error) || string.IsNullOrEmpty(loginResult.PhpSessionId))
{
    FileMgr.Log($"Login failed: {loginResult.Error}");
    ShowLoginError(loginResult.Error ?? "Login failed.");
    return;
}
```

`HourSyncCore.Login` returns 5 values:
- PhpSessionId is the server's session ID. Store this for later use.
- LoginHtmlResponse is only useful if an error has occurred. It is the "home" page upon sign-in.
- StudentName is... the name of the student.
- StudentAcademy is... the name of the academy the student is in.
- Error is null unless an error has occurred.

### Getting the REAL Home Page (list of eHour requests)
Again, quite straightforward, just pass your session ID.
```c#
var getresp = await HourSyncCore.GetRequestsPage(phpSessionId);
```
`HourSyncCore.GetRequestsPage` simply returns the eHours requests page. This is unparsed. Luckily, HourSyncCore can parse them for you.

### Parsing Requests
HourSyncCore parses eHour requests for you into an easy to use class called `EHourRequest`.

You can read the following 5 values:
- Value is the button.value that the Academy Endorsement Portal uses. This is useful for manipulating specific requests.
- Description is the title of your request.
- Hours is how many hours the request was worth.
- Date is... the date it was submitted.
- State is whether it's accepted, denied, pending, or returned. This is not a string, but rather an enum `Status`
- Body will be used in a future update, and it will be the content that you typed out for a request.

Call it like so:
```c#
// Add these lists to the top of your file
private List<Core.EHourRequest> ReturnedRequests = [];
private List<Core.EHourRequest> PendingRequests = [];
private List<Core.EHourRequest> AcceptedRequests = [];
private List<Core.EHourRequest> DeniedRequests = [];

// Parse
var parsed = Core.ParseRequests(getresp);

// And store
ReturnedRequests = parsed.Returned ?? new();
PendingRequests = parsed.Pending ?? new();
AcceptedRequests = parsed.Accepted ?? new();
DeniedRequests = parsed.Denied ?? new();
// Hints:
// Core is whatever you called the using statement from earlier
// getresp is the HTML response from GetRequestsPage()
```

---
HourSyncCore has XML documentations so Visual Studio will pick up on them and help you out.