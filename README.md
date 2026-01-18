# CodeMapper

A fast C# code structure analyzer that generates a hierarchical map of your codebase using Roslyn syntax analysis. Optimized for AI/LLM agents to understand codebases efficiently.

## Features

- 📊 **Public API Surface Only** - Filters to public/internal members only (Goldilocks zone for LLMs)
- 🔍 **Multi-Project Support** - Auto-detects and processes all `.csproj` files
- 📍 **Line Numbers** - Jump directly to code locations
- 🏷️ **Rich Metadata** - Namespaces, base types, interfaces, attributes, static indicators
- 📝 **Smart Documentation** - Extracts first sentence of XML docs (not verbose)
- 📁 **Dual Output** - Text or JSON format
- ⚡ **Fast & Efficient** - Uses Roslyn for accurate syntax analysis
- 🏗️ **AOT Ready** - .NET 9.0 native AOT support

## Installation

### Quick Install (macOS/Linux)
```bash
curl -fsSL https://raw.githubusercontent.com/DennisDyallo/CodeMapper/main/install.sh | bash
```

Or with wget:
```bash
wget -qO- https://raw.githubusercontent.com/DennisDyallo/CodeMapper/main/install.sh | bash
```

**Options:**
- Use `| sudo bash` to install to `/usr/local/bin`
- Set `PREFIX` to customize install location: `PREFIX=$HOME/custom curl ... | bash`
- Set `VERSION` to install specific version: `VERSION=v1.0.0 curl ... | bash`

### Clone & Build
```bash
git clone https://github.com/DennisDyallo/CodeMapper
cd CodeMapper
dotnet build

# Run directly with dotnet
dotnet run -- "/path/to/your/repo"
```

### Publish as Native Executable
```bash
dotnet publish -c Release -o publish
```

After installation, try it out:
```bash
codemapper /path/to/repo
```

## Usage

### Basic Usage
```bash
codemapper /path/to/repo
```

### CLI Options
```
codemapper <path> [options]

Options:
  --format <text|json>    Output format (default: text)
  --output <dir>          Output directory (default: ./codebase_ast)
```

### Examples
```bash
# Analyze repo, output as text
codemapper /path/to/repo

# Output as JSON (better for programmatic use)
codemapper /path/to/repo --format json

# Custom output directory
codemapper /path/to/repo --output ./my-output
```

### Example Output (Text)
```
# Summary: 12 files, 3 namespaces, 24 types, 156 methods

# MyProject/Services/UserService.cs
  [Namespace] MyApp.Services
    [Class] UserService : BaseService, IUserService [ApiController] :15
      [Constructor] UserService(ILogger logger, IRepository repo) :17
      [Property] string Name :22
      [Method:static] User GetUser(int id) :25 // Gets a user by ID.
```

### Example Output (JSON)
```json
{
  "summary": { "files": 12, "namespaces": 3, "types": 24, "methods": 156 },
  "files": [
    {
      "filePath": "Services/UserService.cs",
      "members": [
        {
          "type": "Class",
          "signature": "UserService",
          "lineNumber": 15,
          "baseTypes": ["BaseService", "IUserService"],
          "attributes": ["ApiController"],
          "children": [...]
        }
      ]
    }
  ]
}
```

## What It Maps

| Type | Description |
|------|-------------|
| **Namespaces** | Regular and file-scoped namespaces |
| **Classes** | Public/internal classes with base types |
| **Interfaces** | Interface definitions with inheritance |
| **Records** | Record types with positional parameters |
| **Enums** | Enum declarations with member names |
| **Constructors** | Public constructors (reveals DI dependencies) |
| **Methods** | Signatures with return types and parameters |
| **Properties** | Property declarations with types |
| **Attributes** | Attributes on all public API elements |
| **Static** | Static indicator on classes/methods/properties |
| **Documentation** | First sentence of XML `<summary>` tags |

## Requirements

- .NET 9.0 SDK or runtime

## Testing

```bash
dotnet test
```

## Output

Default output directory: `./codebase_ast/`

Each project generates a file with its code structure (`.txt` or `.json`).

## License

MIT
