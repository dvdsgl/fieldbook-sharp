# FieldBook Client for .NET

### Step 1: Create a Book and a Sheet

Visit [fieldbook.com](http://fieldbook.com) to create a free account and make your first book with a `Tasks` sheet:

![](sheet.png)

### Step 2: Add `fieldbook-sharp` NuGet to your app

Enough said.

### Step 3: Create a class for your sheet

```csharp
class Task : FieldBook.IRow
{
    // Required for IRow interface
    public int Id { get; set; }
    
    public string Name { get; set; }
    public string Notes { get; set; }
    public int Priority { get; set; }
    public bool Finished { get; set; }
}
```

### Step 4: Create your book and sheet objects

Click `Manage API access` inside of FieldBook to get your book ID and create an access key.

```csharp
var book = new Book("book id", "api key name", "api key");
var sheet = book.GetSheet<Task>();
```

### Step 5: Query your sheet!

```csharp
// List all tasks
List<Task> tasks = await sheet.List();

// Get task by Id
Task firstTask = await sheet.Get(0);

// Change a task
firstTask.Priority = 10;
await taskSheet.Update(firstTask);

// Delete a task
await taskSheet.Delete(firstTask);

// Create a task
await taskSheet.Create(new Task
{
    Priority = 1,
    Name = "Build an app",
    Description = "Awesomely with Xamarin",
});
```
