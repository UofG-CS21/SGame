# Team CS21 Main Project - Team Project 3 Course 2019-20

The main focus of the project was to create a space game in which clients will use a REST API to command their spaceship. The server would respond with vague information so that the client would then need to interpret the information and make appropriate judgements and decisions. The idea behind this game would be that clients can be automated and act as load-test clients for the server. The secondary idea is to also allow the more developed client to have a competitive edge over less developed clients. The system should be able to scale horizontally.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on a live system - [See USERGUIDE](USERGUIDE.md).

## Built With

- [dotnet](https://dotnet.microsoft.com/download) - The framework used
- [Gitlab](https://about.gitlab.com/) - DevOps lifecycle tool
- [Elastic Search](https://www.elastic.co/elasticsearch/) - Used for data persistency
- [LiteNetLib](https://revenantx.github.io/LiteNetLib/index.html) - Lite reliable UDP library

### Prerequisites

The prerequisites are as follows:

#### dotnet core 3.0

- XUnit (Unit testing tool for the .NET Framework) - 2.4.0
- LiteNetLib (Lite reliable UDP library for .NET.)
- Newtonsoft.JSon (High-performance JSON framework for .NET.) - 12.0.2
- FxCopAnalyzers (Static analyser) - 2.9.8
- CommandLineParser - 2.6.0

#### Python 3.7 or above

- pytest (Testing framework used for black box tests)
- requests (HTTP library - Used again for testing purposes)

## Installing

A step by step series of examples that tell you how to get a development env running

### Cloning the repositroy

In the directory you wish to clone the repository, run the following command:

For the public GitHub instance:

```
git clone https://github.com/UofG-CS21/SGame.git
```

For the gitlab instance:

```
git clone https://stgit.dcs.gla.ac.uk/tp3-2019-cs21/cs21-main.git
```

### Python Dependencies

pytest and requests can be installed using pip package manager as follows:

```
pip install pytest
```

```
pip install requests
```

### C# Dependencies

restore C# dependencies and build SGame

```C#
dotnet restore SGame
```

```C#
dotnet build SGame
```

## User Guide

Once setup please refer to the user guide for addtional help!

- see the [USERGUIDE](USERGUIDE.md) file for details

## Authors

- **Paolo Jovon** - [paolo.jovon@gmail.com](mailto:paolo.jovon@gmail.com)
- **Samuel Gursky** - [samkogursky@gmail.com](mailto:samkogursky@gmail.com)
- **Matthew Walker** - [mjwalker2299@gmail.com](mailto:mjwalker2299@gmail.com)
- **Martin Nolan** - [martinnolan_1@live.co.uk](mailto:martinnolan_1@live.co.uk)
- **Mustafaa Ahmad** - [mustafaa.ahmad@hotmail.co.uk](mailto:mustafaa.ahmad@hotmail.co.uk)

## License

This project is licensed under the BSD License (4 Clause) - see the [LICENSE](LICENSE) file for details

## Acknowledgments

- Circle-Triangle Intersection Method based on [Gabriel Ivăncescu's implementation](http://www.phatcode.net/articles.php?id=459)
- Lite reliable UDP library for .NET Framework [LiteNetLib link](https://revenantx.github.io/LiteNetLib/index.html)
