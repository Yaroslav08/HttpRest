# HttpRest - extensions for HttpClient
 ### Basic usage

```csharp
var client = new HttpClient();

// Get
var response = await client.GetAsync<List<ToDo>>("https://jsonplaceholder.typicode.com/todos");
// Check result and get List<ToDo>
var result = response.IsSuccess();
var toDos = response.Content; 

var response = await client.PostAsync("https://jsonplaceholder.typicode.com/todos", new PostRequest { Title = "Hello world!" }, compress: true); //use compress

// Download with progress
var response = await client.DownloadAsync(
    "https://api.usn.com/photos/9da94f2f-65fd-49ec-a2ad-9850b72f4ef6.png",
    "photo.png",
    (processed, total) =>
    {
        var percent = (processed * 100) / total;
        Console.WriteLine($"{percent}%");
    });

var response = await client.UploadAsync(
    "https://api.usn.com/photos",
    new List<UploadEntry>
    {
        new UploadEntry(photoStream, "photoMain", "Main.png"),
        new UploadEntry(coverStream, "photoCover", "Cover.png").WithGzip()
    },
    null,
    progress: (processed, total) =>
    {
        var percent = (processed * 100) / total;
        Console.WriteLine($"{percent}%");
    });
```
