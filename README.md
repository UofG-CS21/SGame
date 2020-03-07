# Team CS21 Main Project - Team Project 3 Course 2019-20

The main focus of this project was to create a space game in which clients will use a REST api to command their spaceship. The server would respond with vague information so the client would then need to interpet make appropriate judgments and decisions. The idea behind this game would be that clients can be automated and act as loadtest clients for the server. The secondary idea is to also allow the more developed client to have a competitive edge over less developed clients. The system should be able to scale horizontally.

## API Documentation

<table class="tg">
  <tr>
    <th class="tg-cly1">Name</th>
    <th class="tg-cly1">Parameters</th>
    <th class="tg-cly1">Return Values</th>
    <th class="tg-cly1">Description</th>
  </tr>
  <tr>
    <td class="tg-cly1">connect</td>
    <td class="tg-cly1">None</td>
    <td class="tg-cly1">"token" : new ship token for user (string)</td>
    <td class="tg-cly1">Establishes connection from client to server</td>
  </tr>
  <tr>
    <td class="tg-cly1">disconnect</td>
    <td class="tg-cly1">"token": ship's token (string)</td>
    <td class="tg-cly1">None</td>
    <td class="tg-cly1">Disconnects player whose token is 'token'</td>
  </tr>
  <tr>
    <td class="tg-cly1">accelerate</td>
    <td class="tg-cly1">"token": ship's token (string), " x" : magnitude of acceleration in x-direction (double), "y" : magnitude of acceleration in y-direction (double)</td>
    <td class="tg-cly1">None</td>
    <td class="tg-cly1">Accelerates ship in given direction. Magnitude of accelerations is what the player chose, scaled down if the player does not have enough energy</td>
  </tr>
  <tr>
    <td class="tg-0lax">getShipInfo</td>
    <td class="tg-0lax">"token" : ship's token (string)</td>
    <td class="tg-0lax">"id" : ship's id (string), "area" : ship's area (double) , "energy" : ship's energy (double), "posX" : ship's x position (double), "posY" : ship's y position (double), "velX" : ship's velocity in x-axis (double), "velY" : ship's velocity in y-axis (double), "shieldWidth" : ship's shield width (double), "shieldDir" : ship's shield direction (double)</td>
    <td class="tg-0lax">Returns relevant information of the ship</td>
  </tr>
  <tr>
    <td class="tg-0lax">scan</td>
    <td class="tg-0lax">"token" : ship's token (string), "direction" : direction of scan (double), "width" : width of scan (double), "energy": energy spent on scan (int)</td>
    <td class="tg-0lax">Array of { "id" : struck ship's id (string), "area" : struck ship's area (double), "posX" : struck ship's x position (double), "posY" : struck ship's y position (double) }</td>
    <td class="tg-0lax">Scans a cone around "direction", of width 2 * "width" degrees. The range of the scan depends directly on how much energy is supplied.</td>
  </tr>
  <tr>
    <td class="tg-0lax">shoot</td>
    <td class="tg-0lax">"token" : ship's token (string), "direction" : direction of shot (double), "width" : width of shot (double), "energy": energy to expend on the shot (int), "damage" : the damage -used for scaling (double)</td>
    <td class="tg-0lax">Array of { "id" : struck ship's id (string), "area" : struck ship's area (double), "posX" : struck ship's x position (double), "posY" : struck ship's y position (double) }</td>
    <td class="tg-0lax">Fires an energy cone around "direction", of width 2 * "width" degrees. Power depends on the energy supplied.</td>
  </tr>
  <tr>
    <td class="tg-0lax">shield</td>
    <td class="tg-0lax">"token" : ship's token (string), "direction" : the centre angle of the shield (double), "width" : the half-width of the shield (double)</td>
    <td class="tg-0lax">None</td>
    <td class="tg-0lax">Sets the shield direction and radius around the ship</td>
  </tr>
</table>

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
