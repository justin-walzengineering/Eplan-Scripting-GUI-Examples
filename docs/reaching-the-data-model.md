# Reaching Eplan's data model from a script

Some scripts need to read or change project data: pages, devices, properties. That data lives in Eplan's **data model** (the `Project`, `Page` and `Function` types). This page explains why a script cannot use those types directly, and the pattern the draft examples use instead.

## The problem

Eplan's own documentation lists the assemblies that a script can use without any extra references:

- .NET: `System`, `System.Drawing`, `System.Windows.Forms`, `System.Net.Http`, `System.IO.Compression.FileSystem`, `System.Xml.Linq`, `System.Xml`, `System.Core`, and `Newtonsoft.Json`
- Eplan: Base, ApplicationFramework, Gui and **MasterData**

The data model assembly (`Eplan.EplApi.DataModelu`) and the services assembly (`Eplan.EplApi.HEServicesu`) are **not** on that list, and the documentation says there is no way to add more references from a script. (Adding references is possible only with the separate "Eplan API Extension" license.) So a script that names `Page` or `Project` as a type fails to compile.

Good news: the **MasterData** classes, including the parts database (`MDPartsManagement`, `MDPartsDatabase`, `MDPart`), *are* available directly. Example 11 uses them without any tricks.

## The pattern: reflection

The data model is still loaded inside Eplan while your script runs. **Reflection** is a .NET feature that finds a type or a member by its *name as text*, at run time, instead of at compile time. Instead of:

```csharp
Project project = ...;              // does not compile in a script
string name = project.ProjectName;
```

a script does this:

```csharp
object project = ...;               // some object, type unknown at compile time
string name = Convert.ToString(Get(project, "ProjectName"));   // read the member by name
```

The examples that need the data model (09, 10, 14 and 23) each carry a short helper class that does this safely:

| Helper | What it does |
|--------|--------------|
| `FindType(fullName, assemblyName)` | Finds a type by its full name, loading the assembly if needed |
| `CurrentProject()` | Gets the open project through Eplan's selection service |
| `Get(object, name)` | Reads a property by name, walking up the base classes |
| `GetList(object, name)` | Reads an array property as a list |
| `Call(object, name)` | Calls a method that has no parameters |
| `Text(propertyValue)` | Reads a property value as text, and returns `""` if the read throws |
| `AllProperties(object)` | Every property of an object as `{ id, name, value }` rows |

## Good habits

- **Wrap every read in `try`/`catch`.** Reading an empty property can throw, and one odd object should not stop a whole list.
- **Read only, first.** Reading is safe. Start with read-only tools (examples 09, 10 and 14) before writing anything.
- **Writing needs care.** Example 23 shows the safer pattern for changes: preview, tick boxes, a safety check box, a confirmation, a locking step, and reading each value back afterwards.
- **Unlock the selection.** The selection service returns an empty selection to a script unless `LockSelectionByDefault` is switched off first.
- **Run from a ribbon button or the Quick Access toolbar** when you rely on the selection, so the drawing page keeps focus.
- **Names are specific to a version.** Member names found by reflection can change between Eplan versions, so test after each update.

## What has and has not been confirmed

The member names used by the examples (`Project.Pages`, `Project.ProjectName`, `Page.Name`, `Page.PageTypeName`, `Page.Properties`, `PagePropertyList.PAGE_NOMINATIOMN`, `UniversalPropertyList.ExistingIds` and `ExistingValues`, `SelectionSet.GetCurrentProject`) were checked against the installed 2026 assemblies. The helper classes were tested outside Eplan against stand-in objects of the same shape. **Running them inside Eplan is still to be done**, which is why those examples are marked Draft.
