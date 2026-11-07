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

The helpers were run inside Eplan 2026 (Update 5) by examples 09, 10, 14 and 23, on a copy of a demo project. Reading the project, its pages and their properties works, and so does changing a page description (example 23, confirmed by reading it back afterwards). Two things went wrong in the first drafts and are now fixed in all four examples:

1. **The project could not be opened for reading.** `SelectionSet.GetCurrentProject` failed with "Failed to generate an object of the LockingStep class" until `LockSelectionByDefault` was set to `false` on the selection object first.
2. **Reading the page description by name hit an "ambiguous match" error.** Eplan declares the same property name more than once on its property list classes, so asking .NET for one property by name fails. The helper now walks the list of properties itself and picks the one whose name matches.

Also worth knowing: the member names were checked first against the installed 2026 assemblies (`Project.Pages`, `Project.ProjectName`, `Page.Name`, `Page.PageTypeName`, `Page.Properties`, `PagePropertyList.PAGE_NOMINATIOMN`, `UniversalPropertyList.ExistingIds` and `ExistingValues`, `SelectionSet.GetCurrentProject`) and all of them worked as listed. Not yet working: the **selection of objects on an open drawing page** did not reach the script (example 14 reported nothing selected after Select All on a page), while pages selected in the page navigator did. Other object types, such as devices, have not been tried.
