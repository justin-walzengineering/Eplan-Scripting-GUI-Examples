# C# basics used in these examples

A quick, plain-language tour of the C# ideas you'll see in the examples. You don't need to memorize this. Come back when something looks unfamiliar.

## Comments

Text after `//` is ignored by the computer. It's a note for humans.

```csharp
// This line is a comment.
```

## Variables

A variable is a named box that holds a value.

```csharp
string projectName = "Demo";   // text
int pageCount = 12;            // whole number
bool isOpen = true;            // true or false
```

## Using lines

`using` lines at the top of a file say which toolboxes the code uses. Think of them as "I'll be using tools from here."

```csharp
using System.Windows.Forms;   // windows, buttons, text boxes
```

## Classes and methods

A **method** is a named set of steps. A **class** is a container that groups related methods.

```csharp
public class Greeter                 // the container
{
    public void SayHello()           // a method
    {
        MessageBox.Show("Hello");
    }
}
```

## Windows Forms

Windows Forms is the toolkit for building windows. A window is a **Form**. Things you place on it are **controls**: `Label` (text), `TextBox` (typed input), `Button`, `DataGridView` (a table), and so on.

## Events

An **event** is "something happened", for example a button was clicked. You attach a method that runs when it happens.

```csharp
button.Click += OnButtonClick;       // when clicked, run OnButtonClick
```

## If statements

```csharp
if (name == "")
{
    MessageBox.Show("Please enter a name.");
}
```

## try / catch

Protects against errors so one problem doesn't crash everything.

```csharp
try
{
    // risky work
}
catch (Exception ex)
{
    MessageBox.Show(ex.Message);     // tell the user what went wrong
}
```

## Lists and loops

```csharp
var names = new List<string> { "A", "B" };
foreach (string n in names)
{
    // runs once for each item
}
```

Each example explains the new ideas it introduces where you first see them.
