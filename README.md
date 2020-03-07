# Team CS21 Main Project - Team Project 3 Course 2019-20

The main focus of this project was to create a space game in which clients will use a REST api to command their spaceship. The server would respond with vague information so the client would then need to interpet make appropriate judgments and decisions. The idea behind this game would be that clients can be automated and act as loadtest clients for the server. The secondary idea is to also allow the more developed client to have a competitive edge over less developed clients. The system should be able to scale horizontally.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on a live system.

### Prerequisites

The prerequisites are as follows:

#### donet core 3.0

Unit testing tool for the .NET Framework

```
XUnit
```

Lite reliable UDP library for .NET.

```
LiteNetLib
```

High-performance JSON framework for .NET.

```
Newtonsoft.JSon
```

Static analyser, checks code for security, performance, and design issues, among others

```
FxCopAnalyzers
```

#### Python 2.7 or above

Testing framework used for our black box tests

```
pytests
```

HTTP library (Used again for testing purposes)

```
requests
```

### Installing

A step by step series of examples that tell you how to get a development env running

Say what the step will be

```
Give the example
```

And repeat

```
until finished
```

End with an example of getting some data out of the system or using it for a little demo

## Running the tests

Automated tests can be executed as such:

restore C# dependencies for SGame

```
dotnet restore SGame
```

Builds SGame (integrated FxCop analysis)

```
dotnet build SGame
```

Running the command below runs the automated Xunit tests

```
dotnet test
```

The second set of automated testing which can be executed are the pytests (Black box tests)

```
bash ci/runtests.sh ${SGAME_HOST} ${SGAME_PORT}
```

where SGAME_HOST is the host for the test SGame instance and SGAME_PORT is the port for the test SGame instance

### Break down Xunit tests

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

### And pytest tests

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

## Built With

NEED TO CHANGE EXAMPLES

- [dotnet](https://dotnet.microsoft.com/download) - The framework used

* [Maven](https://maven.apache.org/) - Dependency Management
* [ROME](https://rometools.github.io/rome/) - Used to generate RSS Feeds

## Contributing

Please read [CONTRIBUTING.md]() for details on our code of conduct, and the process for submitting pull requests to us.

## Versioning

MIGHT REMOVE THIS?

## Authors

- **Paolo Jovon** - [UberLambda](https://github.com/UberLambda)
- **Samuel Gursky** - TODO - ADD GITHUB
- **Matthew Walker** - TODO - ADD GITHUB
- **Martin Nolan** - TODO - ADD GITHUB
- **Mustafaa Ahmad** - - [MustafaaAhmad](https://github.com/MustafaaAhmad)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details
NEED TO ADD LICENSE

## Acknowledgments

- Qaudtree basic structure based on [fant12 implementation](https://github.com/fant12/quadTree)
- Circle-Triangle Intersection Method based on [Gabriel Ivăncescu's implementation](http://www.phatcode.net/articles.php?id=459)
