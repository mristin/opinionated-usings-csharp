# Contributing

## Pull Requests

We develop using the feature branches, see [this section of the Git book].

[this section of the Git book]: https://git-scm.com/book/en/v2/Git-Branching-Branching-Workflows

Please prefix the branch with your user name (*e.g.,* `mristin/Add-some-feature`). 
If you want to skip the full battery of CI tests, you can add `doc` or `workflow` qualifier in your branch name (*e.g.*, `mristin/doc/Add-references-to-Readme` or `mristin/workflow/Fix-nuget-publishing`).

If you have write permissions to the repository, create a feature branch directly within the repository.

Otherwise, if you are a non-member contributor, fork the repository and create the feature branch in your forked repository. 
See [this GitHub tutorial] for more guidance.

[this GitHub tutorial]: https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/creating-a-pull-request-from-a-fork

### Commit Messages

The commit messages follow the guidelines from https://chris.beams.io/posts/git-commit:

* Separate subject from body with a blank line
* Limit the subject line to 50 characters
* Capitalize the subject line
* Do not end the subject line with a period
* Use the imperative mood in the subject line
* Wrap the body at 72 characters
* Use the body to explain *what* and *why* (instead of *how*)

## Development Environment

We use `dotnet` command-line tool for all publishing and continuous integration tasks.
Make sure you have .NET core installed.

First, change to the `src/` directory. 
All the subsequent commands should be invoked from there.

### Build

Change to `src/` directory.

The solution is built with:

```bash
dotnet publish --configuration Release --output out
```

The resulting binaries are available in `src/out/` directory

### Continuous Integration

Change to `src/` directory.

You need first to restore the tools:

```bash
dotnet tool restore
```

To re-format the code, run:

```
dotnet format
```

Run all the pre-commit checks

```bash
.\Check.ps1
```

Please see [src/Check.ps1](src/Check.ps1) for more details on individual steps.

The remote workflow is defined as GitHub action in [src/.github/workflows/check.yml](
src/.github/workflows/check.yml
).

### Push Nuget Package 

See [src/.github/workflows/generate-nuget.yml](
src/.github/workflows/generate-nuget.yml
) for how to generate and publish a NuGet package.
