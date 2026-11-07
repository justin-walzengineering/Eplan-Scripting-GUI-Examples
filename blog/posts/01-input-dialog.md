---
title: A reusable input dialog for your Eplan scripts
type: new
example: examples/01-input-dialog
status: ready
excerpt: Ask the user for text, confirm an action and show a message, with three small helpers you can copy into any script.
---

# A reusable input dialog for your Eplan scripts

Many scripts need one small answer from the user: a name, a number, a yes or no. Eplan scripting is one more way to build that into your own tools, and a simple input dialog is a good first window to make.

## What you will build

A short conversation: the script asks for a report name, handles Cancel and empty input politely, asks a Yes/No confirmation, and shows a final message.

## What you will learn

- What a class, a method and the [Start] attribute are
- How to build a window with a label, a text box and buttons
- How a window hands a result back to your code
- Why you always handle Cancel and empty input

## How it works

The example includes a small DialogHelper class with three one-line helpers: Prompt for text, Confirm for Yes/No, and Info for messages.

```
string reportName = DialogHelper.Prompt(
    "New report",
    "What should the report be called?",
    "My report");

if (reportName == null)
{
    DialogHelper.Info("Cancelled", "No report was created.");
    return;
}
```

Prompt returns the typed text, or null if the user pressed Cancel. Checking for null first means pressing Cancel never causes a surprise.

## Try it yourself

- Download the example: https://github.com/justin-walzengineering/Eplan-Scripting-GUI-Examples/tree/main/examples/01-input-dialog
- In Eplan: File > Extras > Interfaces > Run, then select InputDialog.cs.
- Tested in: run in Eplan 2026.0.3; compiles for 2025.0.3 and 2026.0.3 (run in 2025 still to be confirmed).

## Ideas to extend

- Add a second text box, for example a report number
- Validate the input (letters and numbers only, maximum length)
- Use Prompt to ask for a project property value

Repository with all examples: https://github.com/justin-walzengineering/Eplan-Scripting-GUI-Examples

Maintained by Justin Walz, Walz Engineering. Questions, ideas or custom work: get in touch.
