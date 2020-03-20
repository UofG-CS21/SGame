# Team CS21 Main Project - Team Project 3 Course 2019-20

The main focus of the project was to create a space game in which clients will use a REST api to command their spaceship. The server would respond with vague information so the client would then need to interpret and make appropriate judgments and decisions. The idea behind this game would be that clients can be automated and act as loadtest clients for the server. The secondary idea is to also allow the more developed client to have a competitive edge over less developed clients. The system should be able to scale horizontally.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on a live system.

### Prerequisites

The prerequisites are as follows:

#### donet core 3.0

- XUnit (Unit testing tool for the .NET Framework) - 2.4.0
- LiteNetLib (Lite reliable UDP library for .NET.)
- Newtonsoft.JSon (High-performance JSON framework for .NET.) - 12.0.2
- FxCopAnalyzers (Static analyser) - 2.9.8
- CommandLineParser - 2.6.0

#### Python 3.7 or above

- pytests (Testing framework used for our black box tests)
- requests (HTTP library - Used again for testing purposes)

## Installing

A step by step series of examples that tell you how to get a development env running

### Cloning the repositroy

In the directory you wish to clone the repository, run the following command:

For the public GitHub instance:

```
git clone https://github.com/UofG-CS21/SGame.git
```

For the gitlab instance

```
git clone https://stgit.dcs.gla.ac.uk/tp3-2019-cs21/cs21-main.git
```

### Python Dependencies

pytests can be installed using pip package manager as follows:

```
pip install pytest
```

And the same for requests:

```
pip install requests
```

### C# Dependencies

restore C# dependencies for SGame

```C#
dotnet restore SGame
```

Builds SGame

```C#
dotnet build SGame
```

## Running the tests

Automated tests can be executed as such:

restore C# dependencies for SGame

```C#
dotnet restore SGame
```

Builds SGame (integrated FxCop analysis)

```C#
dotnet build SGame
```

Running the command below runs the automated Xunit tests

```C#
dotnet test
```

The second set of automated testing which can be executed are the pytests (Black box tests)

```bash
bash ci/runtests.sh ${SGAME_HOST} ${SGAME_PORT}
```

where SGAME_HOST is the host for the test SGame instance and SGAME_PORT is the port for the test SGame instance

### Xunit tests

The Xunit tests include testing on geometry (scanning, shielding and shooting) and more. The tests were created to test the internal structure of the application.

Below is an example Xunit test:

```csharp
//Test case 1: Attacker is within defending ship. No damage should be shielded.

            gameTime.Reset();
            ship = new Spaceship(1, gameTime);
            ship.Pos = new Vector2(2, 1); //Start ship at (2,1) to avoid missing bugs due to simplicity of (0,0)
            shipRadius = 10;
            ship.Area = shipRadius * shipRadius * Math.PI;

            yield return new object[] { ship, shotOrigin = new Vector2(5, 5), shotDir = -126.869897645844, shotWidth = 1, (ship.Pos - shotOrigin).Length(), expectedValue = 0.0 };

```

### pytest tests

The pytest were created to simulate testing in live manner i.e black box test. Where we would be focus solely on the inputs and outputs with respect to our game specifcation.

For example the test belows ensures that getShipInfo retrieves the correct inital state of the spaceship (without caring about the internals)

```python
def test_getShipInfo_intial_state(clients):
    """
    Tests if getShipInfo matches the intial state of the ship
    """
    with clients(1) as client:
        resp = requests.post(client.url + 'getShipInfo', json={
            'token': client.token,
        })
        assert resp
        resp_data = resp.json()
    # Ensures that the intial values match the information from getShipInfo
        assert resp_data["area"] == 1
        assert resp_data['energy'] == 10
        assert resp_data['posX'] == 0
        assert resp_data['posY'] == 0
        assert resp_data['velX'] == 0
        assert resp_data['velY'] == 0
        assert resp_data['shieldWidth'] == 0
        assert resp_data['shieldDir'] == 0
```

## Deployment

Add additional notes about how to deploy this on a live system

### Data persistency

- If you have elastic search installed, you can addtionally add data persistency:

## User Guide

Once setup please refer to the user guide for addtional help!

- see the [USERGUIDE](USERGUIDE.md) file for details

## Built With

- [dotnet](https://dotnet.microsoft.com/download) - The framework used
- [Gitlab](https://about.gitlab.com/) - DevOps lifecycle tool
- [Elastic Search](https://www.elastic.co/elasticsearch/) - Used for data persistency
- [LiteNetLib](https://revenantx.github.io/LiteNetLib/index.html) - Lite reliable UDP library

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
