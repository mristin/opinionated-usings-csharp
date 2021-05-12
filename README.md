# Opinionated-usings

![Check](
https://github.com/mristin/opinionated-usings-csharp/workflows/Check/badge.svg
) [![Coverage Status](
https://coveralls.io/repos/github/mristin/opinionated-usings-csharp/badge.svg)](
https://coveralls.io/github/mristin/opinionated-usings-csharp
) [![Nuget](
https://img.shields.io/nuget/v/OpinionatedUsings)](
https://www.nuget.org/packages/OpinionatedUsings
)

Opinionated-usings prevents you from generally writing non-aliased [using directives].

[using directives]: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-directive

## Motivation

**Problem**. Many teams write C# code with non-aliased [using directives]:

```
using System.IO.File;
using SomeLibrary;
```

This works really well in the IDE as it tells you immediately type information, but becomes unmanageable in large code bases which are *not* read in an IDE.
For example, If you inspect your code in a diff tool or read it on GitHub.
Unless you are familiar with the code base, you are pretty much lost about from where all the types come.

For example, imagine that `SomeLibrary` in the above snippet provides a class `Something`.
It becomes really hard to trace without an IDE that:

```
var smth = new Something();
smth.doThat();
```

the class `Something` is provided in `SomeLibrary`.
This usually forces the developer to inspect all the dependencies of the code which can be daunting.
If you read your code outside an IDE often, this is a very unnecessary burden. 

**Remedy**. A simple remedy is to alias all the types that you use:

```
using File = System.IO.File;
using Something = SomeLibrary.Something;
```

which tremendously improves the readability in non-IDE setting.

**Exceptions**. However, not all using directives can include an alias.
For example, generics and extensions can not be included explicitly *via* alias:

```
using System.Linq;
```

Additionally, in cases where you need to use types with different names (*e.g.*, you 
need `File` from two dependencies), the aliases need to be proper renames:

```
using File = System.IO.File;
using SomeFile = SomeLibrary.File;
```

**Tool**.
If you have a large code base, you need to handle both the normal case (aliasing without rename) and the exceptions in a systematic way lest the readability suffers.
Opinionated-usings will scan your C# files and let you know whenever a using directive trespasses the conventions.

If you need to use a renaming alias, you need to mark it with `// renamed`:

```
using File = System.IO.File;
using SomeFile = SomeLibrary.File;  // renamed
```

The using directives which are intentionally not aliased are marked with `// can't alias`:

```
using System.Linq;  // can't alias
```

## Related StackOverflow Questions

Here are a couple of StackOverflow questions which prompted the development of the tool:

* https://stackoverflow.com/questions/43383205/why-c-sharp-using-aliases-are-not-used-by-default
* https://stackoverflow.com/questions/147454/why-is-using-a-wild-card-with-a-java-import-statement-bad
* https://stackoverflow.com/questions/3615125/should-wildcard-import-be-avoided

## Installation

Opinionated-usings is available as a dotnet tool with a NET Core 3.1.x runtime.

Either install it globally:

```bash
dotnet tool install -g OpinionatedUsings
```

or locally (if you use tool manifest, see [this Microsoft tutorial on local tools]:

[this Microsoft tutorial on local tools]: https://docs.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use

```bash
dotnet tool install OpinionatedUsings
```

## Usage

### Overview
To obtain an overview of the command-line arguments, use `--help`:

```bash
dotnet opinionated-usings --help
```
<!--- Help starts. -->
```
OpinionatedUsings:
  Examines the using directives in your C# code.

Usage:
  OpinionatedUsings [options]

Options:
  -i, --inputs <inputs> (REQUIRED)    Glob patterns of the files to be inspected
  -e, --excludes <excludes>           Glob patterns of the files to be excluded
  --verbose                           If set, makes the console output more verbose
  --version                           Show version information
  -?, -h, --help                      Show help and usage information
```
<!--- Help ends. -->

### Inputs and Excludes

You run opinionated-usings through `dotnet`.

To obtain help:

```bash
dotnet opinionated-usings --help
```

You specify the files that you want checked using glob patterns:

```bash
dotnet opinionated-csharp-todos --inputs "SomeProject/**/*.cs"
```

Multiple patterns are also possible *(we use '\\' for line continuation here)*:

```bash
dotnet opinionated-usings \
    --inputs "SomeProject/**/*.cs" \
        "AnotherProject/**/*.cs"
```

Sometimes you need to exclude files, *e.g.*, when your solution
contains third-party code which you do not want to scan.

You can provide the glob pattern for the files to be excluded with `--excludes`:

```bash
dotnet opinionated-usings \
    --inputs "**/*.cs" \
    --excludes "**/obj/**"
```

## Contributing

Feature requests, bug reports *etc.* are highly welcome! Please [submit
a new issue](https://github.com/mristin/opinionated-usings-csharp/issues/new).

If you want to contribute in code, please see
[CONTRIBUTING.md](CONTRIBUTING.md).

## Versioning

We follow [Semantic Versioning](http://semver.org/spec/v1.0.0.html).
The version X.Y.Z indicates:

* X is the major version (backward-incompatible w.r.t. command-line arguments),
* Y is the minor version (backward-compatible), and
* Z is the patch version (backward-compatible bug fix).
