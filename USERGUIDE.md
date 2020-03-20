# Team CS21 Main Project - Team Project 3 Course 2019-20

# User Guide

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

## Deployment

Starting in the the top directory of the cloned repository.

restore C# dependencies and build SGame (integrated FxCop analysis)

```C#
dotnet restore SGame
```

```C#
dotnet build SGame
```

First, start a SArbiter instance using:

```C#
dotnet run --project SArbiter  -- --api-url <The HTTP address to serve the REST API on> --bus-port <The UDP port to use for the master event bus>
```

Then SGame nodes can be started and connected to SArbiter using:

```c#
dotnet run --project SGame -- --arbiter <Hostname or address of the SArbiter managing this compute node> --api-url <The HTTP address to serve the SGame REST API on> --arbiter-bus-port <Externally-visible UDP port of the arbiter's event bus> --local-bus-port <Externally-visible UDP port of this node's event bus> --tickrate <Frequency of updates in milliseconds>
```

SArbiter and SGame instances can be killed by sending a post request as shown:
```sh
curl -X POST -d "exit" "<api-url>/exit"
```

Where the api-url is the address used to start the server.

### Data persistency

- If you have elastic search installed, you can addtionally add data persistency:

## Running the tests

Automated tests can be executed as such:

restore C# dependencies and build SGame (integrated FxCop analysis)

```C#
dotnet restore SGame
```

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
