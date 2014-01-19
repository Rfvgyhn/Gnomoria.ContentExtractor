##Overview
* Unpack Gnomoria data files to JSON. Open in any text editor.
* Unpack Gnomoria skins
* Pack Gnomoria skins

##Requirements
* .NET 4 Full ([web](http://www.microsoft.com/downloads/details.aspx?FamilyID=9cfb2d51-5ff4-4491-b0e5-b386f32c0992&displaylang=en) | [standalone](http://www.microsoft.com/downloads/details.aspx?displaylang=en&FamilyID=0a391abd-25c1-4fc0-919f-b21f31ab88b7))
* In order to unpack the data files, you will need to provide a de-obfuscated `gnomorialib.dll` file. You manually copy this file to the `lib` folder. This isn't required if you just want to pack/unpack skins.

## Switches
| Switch   | Alias | Value       | Description         | Required |
| -------- | ----- | ----------- | ------------------- | -------- |
| --action | -a    | unpack/pack | Pack or unpack XNB  | Yes      |
| --type   | -t    | data/skin   | Type to pack/unpack | No       |
| --input  | -i    | <path>      | Input file/folder   | No       |
| --output | -o    | <path>      | Output destination  | Yes      |
| --help   | -?    |             | Display help screen |          |

## Examples
### Unpack Data Files
`GnomoriaContentExtractor -a unpack -t data -o C:\Gnomoria\Content\Data`

`GnomoriaContentExtractor -a unpack -t data -i C:\path\to\gnomoria\data\folder -o C:\Gnomoria\Content\Data`

### Unpack Skins
`GnomoriaContentExtractor -a unpack -t skin -o C:\Gnomoria\Content\UI`

`GnomoriaContentExtractor -a unpack -t skin -i C:\path\to\gnomoria\ui\folder -o C:\Gnomoria\Content\UI`

### Pack Skins
`GnomoriaContentExtractor -a pack -t skin -i C:\path\to\modified\skin\folder -o C:\Gnomoria\Content\UI\Custom.skin`

## Other
When unpacking data or skins, you may leave out the `-i` parameter if you have the Steam version installed in the default location. The tool will look in `%ProgramFiles(x86)%\Steam\steamapps\common\Gnomoria\Content\` (defined in `app.config` which you can change) for the proper files.