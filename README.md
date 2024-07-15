# EPR Common

## Overview
Shared package for common functions.
- `EPR.Common.Authorization` - common code about authorization, policies, service roles, user and organisation data operations.
- `EPR.Common.Functions` - contains basic database entities, repositories, unit of work. Also has authentication and refresh token HTTP Post endpoints. 
- `EPR.Common.Logging` - common entrypoint for other services to provide methods for protective monitoring logging.

## How To Run

### Prerequisites
In order to run the service you will need the following dependencies
- .NET 6
- Azure CLI

### Build
On root directory `EPR.Common`, execute:
```
dotnet build
```

### Run
This solution contains only .NET libraries. It cannot be run directly, but can be used as a nuget package in other services.

### Docker
N/A

## How To Test

### Unit tests

On root directory `EPR.Common`, execute:
```
dotnet test
```

### Pact tests
N/A

### Integration tests
N/A

## How To Debug
N/A

## Additional Information
See [User Authorization Middleware](https://eaflood.atlassian.net/wiki/spaces/MWR/pages/4346839200/User+Authorization+Middleware)

## Directory Structure
### Source files
- `EPR.Common.Authorization` - Authorization .NET source files
- `EPR.Common.Authorization.Test` - .NET unit test files
- `EPR.Common.Functions` - Functions .NET source files
- `EPR.Common.Functions.Test` - .NET unit test files
- `EPR.Common.Logging` - Logging .NET source files
- `EPR.Common.Logging.Tests` - .NET unit test files

## Contributing to this project
Please read the [contribution guidelines](CONTRIBUTING.md) before submitting a pull request.

## Licence
[Licence information](LICENCE.md).

