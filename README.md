# HourSyncCore

_This project is licensed under the Apache License 2.0. All code, including previous commits, is now licensed under the terms of the Apache License, Version 2.0._

---

The elephant in the room: yes, 99% of the code overlaps with previous releases of HourSync. This was an easy way to trim some fat in the HourSync app and an easy way to allow others to use my work.

# What is this?

HourSyncCore is a way to interface with the Academy Endorsement (colloquially called the eHours portal) website by the Olathe School District.

It interfaces easily with the website and makes it stupid simple for you to make your own version of HourSync.

_**The setup I've used below uses C# and WinUI 3 XAML. If you wish to use HourSyncCore in another style, you may have some extra setup, which I am not responsible for. You can fork this repository if you want to change it.**_

# Features

-   Logging into the portal
-   Getting the eHours page (aka the home page)
-   Parsing the home page in an easy to manipulate format
-   Fetching specific requests
-   Editing pending requests
-   Fetching the leaderboard

Making requests will come in a future update.

# Usage

Really easy.

This package is now on NuGet. Right click on your project in Visual Studio and click `Manage NuGet Packages...`. Search for HourSyncCore and install it.

Add the following using statement to the top of each file you use HourSync in:

```c#
using HourSyncCoreLib;
```

### Logging in

Quite straightforward:

```c#
var loginResult = await HourSyncCore.Login(username, password);

if (!string.IsNullOrEmpty(loginResult.Error) || string.IsNullOrEmpty(loginResult.PhpSessionId))
{
    FileMgr.Log($"Login failed: {loginResult.Error}");
    ShowLoginError(loginResult.Error ?? "Login failed.");
    return;
}
```

`HourSyncCore.Login` returns 5 values:

-   PhpSessionId is the server's session ID. Store this for later use.
-   LoginHtmlResponse is only useful if an error has occurred. It is the "home" page upon sign-in.
-   StudentName is... the name of the student.
-   StudentAcademy is... the name of the academy the student is in.
-   Error is null unless an error has occurred.

### Getting the List of past eHour Requests

Again, quite straightforward, just pass your session ID.

```c#
var getresp = await HourSyncCore.GetRequestsPage(phpSessionId);
```

`HourSyncCore.GetRequestsPage` simply returns the eHours requests page. This is unparsed. Luckily, HourSyncCore can parse them for you.

### Parsing Requests

HourSyncCore parses eHour requests for you into an easy to use class called `EHourRequest`.

You can read the following 5 values:

-   Value is the button.value that the Academy Endorsement Portal uses. This is useful for manipulating specific requests.
-   Description is the title of your request.
-   Hours is how many hours the request was worth.
-   Date is... the date it was submitted.
-   State is whether it's accepted, denied, pending, or returned. This is not a string, but rather an enum `Status`
-   Body will be used in a future update, and it will be the content that you typed out for a request.

Call it like so:

```c#
// Add these lists to the top of your file
private List<EHourRequest> ReturnedRequests = [];
private List<EHourRequest> PendingRequests = [];
private List<EHourRequest> AcceptedRequests = [];
private List<EHourRequest> DeniedRequests = [];

// Parse
var parsed = Core.ParseRequests(getresp);

// And store
ReturnedRequests = parsed.Returned ?? new();
PendingRequests = parsed.Pending ?? new();
AcceptedRequests = parsed.Accepted ?? new();
DeniedRequests = parsed.Denied ?? new();

// Hint:
// getresp is the HTML response from GetRequestsPage()
```

Now you can use the description property (which is technically the title of the event) to create a layout:

```xml
<!-- Add the following to the top of your XAML file, inside the Window or Page tag -->
xmlns:core="using:HourSyncCoreLib"

<!-- Master requests panel -->
<StackPanel x:Name="RequestsPanel" Margin="0,0,0,10">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="Pending Requests -" Margin="0,0,5,0"/>

        <!-- Display how many requests there are in this category -->
        <TextBlock Text="{x:Bind PendingRequests.Count, Mode=OneWay}"/>
    </StackPanel>

    <!-- From my experience, the buttons appear too far right so the margin is negative to shift it left -->
    <ListView ItemsSource="{x:Bind PendingRequests, Mode=OneWay}" SelectionMode="None" Margin="-15, 5, 0, 0">
        <ListView.ItemTemplate>
            <DataTemplate x:DataType="core:EHourRequest">
                <!-- The Tag property of the button is invisible to the end user, but makes your job SO much easier when handling click events -->

                <!-- Make sure to change the click event -->
                <Button
                    Tag="{x:Bind Value}"
                    HorizontalAlignment="Stretch"
                    HorizontalContentAlignment="Left"
                    Height="80"
                    Margin="0,0,0,10"
                    Click="RequestButton_Click">
                    <Button.Content>
                        <!-- I personally like to show users as much useful info as possible. You cannot use  -->
                        <TextBlock HorizontalAlignment="Left" TextWrapping="Wrap">
                            <Run Text="{x:Bind Description}"/>
                            <LineBreak/>
                            <Run Text="Hours:"/>
                            <Run Text="{x:Bind Hours}"/>
                            <LineBreak/>
                            <Run Text="Date:"/>
                            <Run Text="{x:Bind Date}"/>
                        </TextBlock>
                    </Button.Content>
                </Button>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>

    <!-- copy and paste for the other three statuses -->
    <!-- make sure to change the textblock text and the itemssource -->
</StackPanel>

```

### Fetching specific requests

HourSyncCore has a FetchedRequest result for `HourSyncCore.GetRequest`, which provides the following:

-   Value is the unique identifier for a request
-   Date is pretty obvious
-   Body is also pretty obvious: the content you typed
-   RequestedHours is a STRING, and should be easy to figure out what it is
-   Images is a string list of the image URLs from a request
-   Comments are... comments... from the teacher. I'm still yet to find a use for it
-   Success is a bool that tells you whether the request was successful or not
-   Error will have a value if something went wrong.
-   LoggedIn is a bool that helps to figure out why Success may be false

The one thing which HourSyncCore can NOT return is the title of the event; this is simply because the official portal does not have it on the request page for some odd reason. After using `Core.ParseRequests` as you should've in the previous step, you can map the event title to the request ID, or the other way around.

In the previous step, if you used the XAML binding, you can use the following code:

```c#
// I'd hope you're already using these using statements:
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// or whatever your request button click event is
private void RequestButton_Click(object sender, RoutedEventArgs e)
{
    Button clickedButton = (Button)sender;
    string value = (string)clickedButton.Tag;
    // value is now the request's unique identifier

    // Now you can get the request
    FetchedEHourRequest response = await HourSyncCore.GetRequest(phpSessionId, value, true);
}
```

When submitting requests, Windows may introduce a dynamic quote (or sometimes other dynamic punctuation), which is not compatible with the encoding the server uses. When fetching requests via the portal or HourSync, this is reflected in the Body, where you may see stuff like `�??`. You can pass in the third argument as `true` or `false` to have HourSyncCore fix some of these funky characters (as ChatGPT called it, mojibake).

All of that aside, here's how you'd use it:

```c#
// you can use "var" in new versions of c#
FetchedEHourRequest response = await HourSyncCore.GetRequest(phpSessionId, id, true);
```

Here's an implementation of the GetRequest method:

```c#
if (response.Success){
    //do something:
    //note, HourSyncCore does NOT have these functions
    //these are just ideas as to what you can do with the returned values
    DescriptionBlock.Text = response.Body;
    DateBlock.Text = RelativeDateAndHoursParser.Parse(response.Date, response.RequestedHours);
    if (response.Images.Length>0){
        // Create a base URL for relative paths, since they are returned relatively
        // means image path is "../{image}.png", not "httsp://{website}.com/{image}.png"
        const string baseUrl = "https://academyendorsement.olatheschools.com/";

        foreach(string image in imagePaths){
            CreateAndAddImages(image)
        }
    }
}
```

### Editing pending requests

This method is really simple to use because HourSyncCore provides an UpdatedRequest record for you to use:

```c#
UpdatedRequest updated = new (){
    Value = MyRequest.Value, // the unique identifier of the request
    NewContent = EditBox.Text, // the new content
    State = MyRequest.State // I would highly recommend NOT lying about this so that HourSyncCore can ensure that the request is actually editable. You can technically pass Status.Pending though.
}
```

Then, just call:

```c#
PostResponse response = HourSyncCore.EditRequest(phpSessionId, updated)
```

and it's done. HourSyncCore will only return if the operation was successful, any error, if you're logged in, and the HTML response.

### Leaderboard

The leaderboard is an interesting addition to the portal and, at least for my academy, has some ghost users. Despite this, HourSyncCore provides two records for you to use: `LeaderboardStudent` and `LeaderboardData`. `LeaderboardData` is a list of `LeaderboardStudent` records.

The LeaderboardStudent record provides the name of the student as a string, the number of eHours they have _**as a string**_, and their rank (place) as an integer.

All you need to pass is your phpSessionId, and you can use x:Bind to make it easy to display the data:

```c#
//Add this in your Page or Window c#:
private LeaderboardData _leaderboardData;

public LeaderboardData leaderboardData
{
    get => _leaderboardData;
    set
    {
        _leaderboardData = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(leaderboardData)));
    }
}

public event PropertyChangedEventHandler PropertyChanged;

//Then get the data
leaderboardData = await HourSyncCore.FetchLeaderboard(phpSessionId);
```

And here's the XAML:

```xml
<!-- Add the following to the top of your XAML file, inside the Window or Page tag -->
xmlns:core="using:HourSyncCoreLib"

<StackPanel Orientation="Vertical" Spacing="10">
    <!-- Title -->
    <TextBlock Text="Leaderboard"
           Style="{ThemeResource TitleTextBlockStyle}"
           Margin="0,0,0,10"/>

    <!-- List of students -->
    <StackPanel Orientation="Horizontal" Spacing="20" Margin="14,0,0,0">
        <TextBlock Text="#"
               Style="{ThemeResource BodyStrongTextBlockStyle}"/>
        <TextBlock Text="Name"
           Style="{ThemeResource BodyStrongTextBlockStyle}"
           Width="250"/>
        <TextBlock Text="# of Hours"
           Style="{ThemeResource BodyStrongTextBlockStyle}"/>
    </StackPanel>

    <!-- If you want to do something with selecting students, change the SelectionMode property -->
    <ListView ItemsSource="{x:Bind leaderboardData.StudentData, Mode=OneWay}" SelectionMode="Single">
        <ListView.ItemTemplate>
            <DataTemplate x:DataType="core:LeaderboardStudent">
                <StackPanel Orientation="Horizontal" Spacing="20">
                    <TextBlock Text="{x:Bind Rank}"
                               Style="{ThemeResource BodyStrongTextBlockStyle}"/>
                    <TextBlock Text="{x:Bind Name}"
                           Style="{ThemeResource BodyTextBlockStyle}"
                           Width="250"/>
                    <TextBlock Text="{x:Bind Hours}"
                           Style="{ThemeResource BodyTextBlockStyle}"/>
                </StackPanel>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</StackPanel>
```

---

HourSyncCore has XML documentations so Visual Studio intellisense will pick up on them and help you out.

Eventually, there will be a complete app that you can copy and paste, that has the barebones functions of HourSyncCore. Until then, just ask ChatGPT or read the docs yourself.

# License

HourSyncCore is licensed under the same license as HourSync, which is the Apache 2.0 license.
