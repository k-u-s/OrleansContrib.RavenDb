
# OrleansContrib.RavenDb

TODO: Add badges with links to version, test coverage etc.

[RavenDb](https://github.com/ravendb/ravendb) implementation of the [Orleans](https://github.com/dotnet/orleans) Providers.

This includes

 - [Storage](https://dotnet.github.io/orleans/docs/grains/grain_persistence/index.html) (IGrainStorage)
 - [Reminders](https://dotnet.github.io/orleans/docs/grains/timers_and_reminders.html#configuration) (IReminderTable)

### Disclaimer

Pretty much whole test suit is copied from main Orleans repo. More or less source for mentioned tests [reminders](https://github.com/dotnet/orleans/blob/bf807fdb8c39157f9ff73490b8368c94b9b64b2b/test/TesterInternal/RemindersTest/ReminderTableTestsBase.cs) and [persistence](https://github.com/dotnet/orleans/blob/bf807fdb8c39157f9ff73490b8368c94b9b64b2b/test/TesterInternal/StorageTests/CommonStorageTests.cs)

## Installation

TODO: add nuget install cmd

## Setup

All steps assume that you have running ravendb instance available on localhost on default ports.

### Storage

TODO: add sample code that shows how to register storage provider

### Reminders

TODO: add sample code that shows how to register reminder table

## Contributions

PRs and feedback are very welcome!

## License

This project is released under the [MIT license](https://github.com/k-u-s/OrleansContrib.RavenDb/blob/master/LICENSE).
