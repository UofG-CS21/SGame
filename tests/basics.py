import requests
import time
import pytest

allowed_fpe = 1e-6

def is_close(a, b, err=allowed_fpe):
    return abs(a-b) <= err


def reset_time(server):
    """Call at the BEGINNING of a test if you want it to use manual time. Time will be set to `time`."""
    # set time to 0
    resp = requests.post(server.url + 'sudo', json={
        'time': 0,
    })
    assert resp


def set_time(server, time):
    resp = requests.post(server.url + 'sudo', json={
        'time': time,
    })
    assert resp


def test_disconnect(clients):
    """
    Tests that a connected ship can disconnect via REST.
    """
    # Content manager automatically connects and disconnects
    with clients(1) as client:
        resp = requests.post(client.url + 'getShipInfo', json={
            'token': client.token,
        })
        assert resp
    # Client should successfully disconnect after getShipInfo has been called


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
        assert resp_data['velX'] == 0
        assert resp_data['velY'] == 0
        assert resp_data['shieldWidth'] == 0
        assert resp_data['shieldDir'] == 0


def test_scan(server, clients):
    # Create two clients
    reset_time(server)
    with clients(2) as (client1, client2):

        resp = requests.post(client1.url + 'getShipInfo', json={
            'token': client1.token,
        })
        assert resp
        # Getting the id for client 1
        resp_data = resp.json()
        client1_id = resp_data['id']

        # Reset clients to center
        resp = requests.post(client1.url + 'sudo', json={
            'token': client1.token,
            'posX': 0.0,
            'posY': 0.0,
        })
        assert resp
        resp = requests.post(client2.url + 'sudo', json={
            'token': client2.token,
            'posX': 0.0,
            'posY': 0.0,
        })
        assert resp

        # Client 1 moves to the right
        resp = requests.post(client1.url + 'accelerate', json={
            'token': client1.token,
            'x': 1,
            'y': 0,
        })
        assert resp

        # Waiting 1 second
        set_time(server, 1000)

        # Stop previous acceleration for client 1
        resp = requests.post(client1.url + 'accelerate', json={
            'token': client1.token,
            'x': -1,
            'y': 0,
        })
        assert resp

        # Client 2 scans the area where client 1 should be
        resp = requests.post(client2.url + 'scan', json={
            'token': client2.token,
            'direction': 0,
            'width': 45,
            'energy': 5,
        })
        assert resp
        resp_data = resp.json()

        print('scan that should not fail:', resp_data)
        # Checking if client 1 id is found in the response
        assert any(scanned['id'] ==
                   client1_id for scanned in resp_data['scanned'])

        # Scan should not discover ship in empty area
        resp = requests.post(client2.url + 'scan', json={
            'token': client2.token,
            'direction': 180,
            'width': 45,
            'energy': 5,
        })
        assert resp
        resp_data = resp.json()
        # Checking that scan doesnt find any ships in that area
        print('scan that should fail:', resp_data)
        assert not any(scanned['id'] ==
                       client1_id for scanned in resp_data['scanned'])


def test_movement(server, clients):
    """
    Tests that accelerate/ movement such that a ship can accelerate using an x and y.
    """

    reset_time(server)
    with clients(1) as client:
        # Getting the intial ship info
        resp = requests.post(server.url + 'getShipInfo', json={
            'token': client.token,
        })
        assert resp

        # Checking that the velocity is 0 at the start
        resp_data = resp.json()
        assert resp_data['velX'] == 0
        assert resp_data['velY'] == 0
        assert resp_data['energy'] == 10

        # Test values
        x1 = 2.6
        y1 = 2.5
        x2 = 1.5
        y2 = 2.3
        sumX = x1 + x2
        sumY = y1 + y2

        # Calling accelerate with first values
        resp = requests.post(server.url + 'accelerate', json={
            'token': client.token,
            'x': x1,
            'y': y1,
        })
        assert resp

        # Calling accelerate with second values
        resp = requests.post(server.url + 'accelerate', json={
            'token': client.token,
            'x': x2,
            'y': y2,
        })
        assert resp

        # Using getShipInfo to check if the values match the expected result
        resp = requests.post(server.url + 'getShipInfo', json={
            'token': client.token,
        })
        assert resp

        # Checking the values are correct
        resp_data = resp.json()
        assert is_close(resp_data['velX'], sumX)
        assert is_close(resp_data['velY'], sumY)

        # Wait for energy to recharge
        set_time(server, 10000)

        resp = requests.post(server.url + 'getShipInfo', json={
            'token': client.token,
        })
        assert is_close(resp.json()['energy'], 10)

        # Accelerate in the opposite direction
        resp = requests.post(server.url + 'accelerate', json={
            'token': client.token,
            'x': -x1,
            'y': -y1,
        })
        assert resp

        resp = requests.post(server.url + 'accelerate', json={
            'token': client.token,
            'x': -x2,
            'y': -y2,
        })
        assert resp

        # Using getShipInfo to check if the values match the expected result
        resp = requests.post(server.url + 'getShipInfo', json={
            'token': client.token,
        })
        assert resp

        resp_data = resp.json()
        assert is_close(resp_data['velX'], 0)
        assert is_close(resp_data['velY'], 0)

        # Wait for energy to recover
        set_time(server, 20000)

        resp = requests.post(server.url + 'getShipInfo', json={
            'token': client.token,
        })
        assert is_close(resp.json()['energy'], 10)

        # Accelerate requiring too much energy
        X = 90.0
        Y = -10.0

        resp = requests.post(server.url + 'accelerate', json={
            'token': client.token,
            'x': X,
            'y': Y,
        })
        assert resp

        # Check that we used up all energy and accelerated a proportion of what we asked for
        resp = requests.post(server.url + 'getShipInfo', json={
            'token': client.token,
        })
        assert resp

        resp_data = resp.json()
        assert is_close(resp_data['energy'], 0, 0.01)
        assert is_close(resp_data['velX'], 9.0)
        assert is_close(resp_data['velY'], -1.0)

        old_x = resp_data['posX']
        old_y = resp_data['posY']

        # give the ship time to move
        set_time(server, 24500)

        # check that it moved the correct amount
        resp = requests.post(server.url + 'getShipInfo', json={
            'token': client.token,
        })
        assert resp

        resp_data = resp.json()
        assert is_close(resp_data['posX'], old_x + 9.0 * 4.5, 0.5)
        assert is_close(resp_data['posY'], old_y + (-1.0) * 4.5, 0.5)

        # Disconnect
        resp = requests.post(server.url + 'disconnect',
                             json={'token': client.token})
        assert resp


def test_sudo(server, clients):
    """
    Tests that the "sudo" endpoint actually sets values properly.
    """
    # NOTE: Can only reliably set velocity and ares as they are the the only
    #       values that do not change over time (for now)!
    kvs = {
        'velX': 1010,
        'velY': 4242,
        'area': 123456,
    }

    with clients(1) as client:
        # Assert "initial state" != "state to set"
        # (to make sure values are actually changed)
        resp = requests.post(server.url + 'getShipInfo',
                             json={'token': client.token})
        assert resp
        init_state = resp.json()
        for k, v in kvs.items():
            assert k in init_state
            assert not is_close(v, init_state[k], 0.001)

        # Change all values to the expected ones
        json = {'token': client.token}
        json.update(kvs)
        print('sudo payload:', json)
        resp = requests.post(server.url + 'sudo', json=json)
        assert resp

        # Assert "state after set" == "state to set"
        resp = requests.post(server.url + 'getShipInfo',
                             json={'token': client.token})
        assert resp
        for k, v in resp.json().items():
            if k not in kvs:
                continue
            assert is_close(v, kvs[k], 0.001)


def test_sudo_bad_or_missing_token(server):
    """
    Tests that the "sudo" endpoint fails if no valid token is passed.
    """
    resp = requests.post(server.url + 'sudo',
                         json={'token': '**NOT_A_VALID_TOKEN**'})
    assert not resp

    resp = requests.post(server.url + 'sudo',
                         json={})
    assert resp


def test_basic_combat(server, clients):
    with clients(2) as (client1, client2):
        # Setting up client 1
        resp = requests.post(client1.url + 'sudo', json={
            'token': client1.token,
            'posX': 0,
            'posY': 0,
            'area': 2,
            'energy': 20
        })
        assert resp

        # Setting up client 2
        resp = requests.post(client2.url + 'sudo', json={
            'token': client2.token,
            'posX': 2.5,
            'posY': 2.5,
            'area': 10,
        })
        assert resp

        # Getting client 2 area
        resp = requests.post(client2.url + 'getShipInfo', json={
            'token': client2.token,
        })
        assert resp
        resp_data = resp.json()
        client2_area_before = resp_data['area']

        # Client 1 shooting client 2
        resp = requests.post(client1.url + 'shoot', json={
            'token': client1.token,
            'direction': 0,
            'width': 45,
            'energy': 10,
            'damage': 1.5,
        })
        assert resp

        # Getting client 1 info
        resp = requests.post(client1.url + 'getShipInfo', json={
            'token': client1.token,
        })
        assert resp
        resp_data = resp.json()

        client1_area = resp_data['area']
        client1_energy = resp_data['energy']
        # Checking client 1 info is correct
        assert client1_energy <= 6  # calculation below!
        assert client1_area == 2

        # Getting client 2 info
        resp = requests.post(client2.url + 'getShipInfo', json={
            'token': client2.token,
        })
        assert resp

        resp_data = resp.json()
        client2_area = resp_data['area']
        # Checking damage is taken off
        # hand checked damage cal:
        # energy = min(10, 20/1.5) = 10
        # ships energy = 20- 15 = 5~
        # shot damage(10, 45, 1.5, (Magnitude of distance) 3.53...)
        # width = pi/4
        # damage = (10*1.5)/ (2.97.. * sqrt(3.53)) = 2.68...
        assert is_close(client2_area, client2_area_before - 2.685387372970581)


# Dataset for death test
test_death_data = [
    # FORMAT: client1 posX, client1 posY, client1 area, client1 energy, client2 posX, client2 posY, client2 area ,shoot dir ,shoot width ,shoot energy, damage scaling
    (0, 0, 25, 50, 5, 5, 5, 0, 45, 15, 10),
    (2.5, 3, 30, 50, 6, 4, 8, 0, 30, 15, 9),
    (4, 4, 40, 40, 8, 4, 4, 0, 25, 20, 8),
    (100, 0, 50, 100, -50, 0, 5, 180, 20, 70, 10),
]
@pytest.mark.parametrize("client1_x, client1_y, client1_area, client1_energy, client2_x, client2_y, client2_area, direction, width, energy , damage", test_death_data)
def test_combat_death(server, clients, client1_x, client1_y, client1_area, client1_energy, client2_x, client2_y, client2_area, direction, width, energy, damage):
    with clients(2) as (client1, client2):
        # Setting up client1
        resp = requests.post(client1.url + 'sudo', json={
            'token': client1.token,
            'posX': client1_x,
            'posY': client1_y,
            'area': client1_area,
            'energy': client1_energy,
        })
        assert resp

        # Setting up client2
        resp = requests.post(client2.url + 'sudo', json={
            'token': client2.token,
            'posX': client2_x,
            'posY': client2_y,
            'area': client2_area,
        })
        assert resp

        # Shooting with data
        resp = requests.post(client1.url + 'shoot', json={
            'token': client1.token,
            'direction': direction,
            'width': width,
            'energy': energy,
            'damage': damage,
        })
        assert resp

        # Making sure client2 gets 500 response as client2's ship is dead
        resp = requests.post(client2.url + 'getShipInfo', json={
            'token': client2.token,
        })
        resp_data = resp.json()
        assert 'error' in resp_data.keys()
        assert resp_data['error'] == "Your spaceship has been killed. Please reconnect."
        assert resp.status_code == 500


def test_kill_reward(server, clients):
    # Control time manually to get around LastCombat
    reset_time(server)
    with clients(2) as (client1, client2):
        # Setting up client 1
        resp = requests.post(client1.url + 'sudo', json={
            'token': client1.token,
            'posX': 0,
            'posY': 0,
            'area': 30,
            'energy': 200,
        })
        assert resp

        # Setting up client2
        resp = requests.post(client2.url + 'sudo', json={
            'token': client2.token,
            'posX': 5,
            'posY': 5,
            'area': 20,
        })
        assert resp

        # Moving forward, going over COMBAT_COOLDOWN
        set_time(server, 100000)

        # Shooting once
        resp = requests.post(client1.url + 'shoot', json={
            'token': client1.token,
            'direction': 0,
            'width': 45,
            'energy': 50,
            'damage': 10,
        })
        assert resp

        resp = requests.post(client1.url + 'getShipInfo', json={
            'token': client1.token,
        })
        assert resp

        resp_data = resp.json()
        # Checking the first ship gains the killreward
        assert resp_data['area'] == 50


# Test to check another ship can steal a kill
def test_kill_steal(server, clients):
    reset_time(server)
    with clients(3) as (client1, client2, client3):

        # Setting up client 1
        resp = requests.post(client1.url + 'sudo', json={
            'token': client1.token,
            'posX': 0,
            'posY': 0,
            'area': 20,
            'energy': 200,
        })
        assert resp

        # Setting up client2
        resp = requests.post(client2.url + 'sudo', json={
            'token': client2.token,
            'posX': -200,
            'posY': 0,
            'area': 20,
            'energy': 200,
        })
        assert resp

        # Setting up client3
        resp = requests.post(client3.url + 'sudo', json={
            'token': client3.token,
            'posX': 2,
            'posY': 0,
            'area': 100,
        })
        assert resp

        set_time(server, 100000)

        # Shooting once and dealing damage of 98~
        resp = requests.post(client1.url + 'shoot', json={
            'token': client1.token,
            'direction': 0,
            'width': 15,
            'energy': 50,
            'damage': 10,
        })
        assert resp

        # Moving client 1 away
        resp = requests.post(client1.url + 'sudo', json={
            'token': client1.token,
            'posX': -200,
            'posY': -200,
        })

        # client2 moved into combat
        resp = requests.post(client2.url + 'sudo', json={
            'token': client2.token,
            'posX': 1,
            'posY': 0,
        })
        assert resp

        # Shooting once and dealing damage of over 150~ to ensure client 2 steals the kill
        resp = requests.post(client2.url + 'shoot', json={
            'token': client2.token,
            'direction': 0,
            'width': 10,
            'energy': 50,
            'damage': 10,
        })
        assert resp

        # Checking client 1 gets the right area
        resp = requests.post(client1.url + 'getShipInfo', json={
            'token': client1.token,
        })
        assert resp
        resp_data = resp.json()
        # Client one doesnt gain the area
        assert resp_data['area'] == 20

        # Checking client 2 gains client 3 area
        resp = requests.post(client2.url + 'getShipInfo', json={
            'token': client2.token,
        })
        assert resp
        resp_data = resp.json()
        # Kill reward does not add properly
        assert resp_data['area'] == 120

        # Client 3 dies
        resp = requests.post(client3.url + 'getShipInfo', json={
            'token': client3.token,
        })

        resp_data = resp.json()
        assert 'error' in resp_data.keys()
        assert resp_data['error'] == "Your spaceship has been killed. Please reconnect."


# Test data for the fixture
testdata = [

    # FORMAT : scandir, scan_width, posX_s2, posY_s2, area_s2, area_s1, energy, expected

    # Case 1: ship is on top of the other ship
    (0, 30, 0, 0, 10, 1, 5, True),

    # Case 2: Ship adjacent and should be detected
    (0, 30, 2, 0, 2, 1, 5, True),

    # Case 3: Test for ship outside scan region i.e scanning the opposite direction
    (180, 30, 2, 0, 2, 1, 5, False),

    # Case 4: Ship center is within circular segment, but not touching it or the triangle
    (0, 45, 850, 0, 6, 103, 1000, True),

    # Case 5: Ship outwith circular segment
    (0, 45, 1500, 0, 10, 100, 1000, False),

    # Case 6: Ship inside of the scan
    (0, 15, 900, 0, 10, 100, 1000, True),

    # Case 7: Triangle vertex within ship
    (0, 15, 1887.8151, 506, 10, 100, 1000, True),

    # Case 8: Triangle vertex within ship
    (0, 15, 1887.8151, -506.838353, 10, 100, 1000, True),

    # Case 9: Ship on lower boundary
    (0, 15, 1699.03, -456.15, 10, 100, 1000, True),

    # Case 10: Ship on upper boundary
    (0, 15, 1699.03, 456.15, 10, 100, 1000, True),

    # Case 11: Ship is on the end boundry of the scan
    (0, 45, 1129.37, 0, 5, 100, 1000, True),

    # Case 12: Ship is behind the scan area
    # Ship is behind and below
    (0, 30, -1196.8268, -690.9883, 5, 100, 1000, False),
    # Ship is behind and above
    (0, 30, -1196.8268, 690.9883, 5, 100, 1000, False)
]

# Test to check scan works correctly with the use of test data and the SUDOApi
@pytest.mark.parametrize("scandir, scan_width, posX_s2, posY_s2, area_s2, area_s1, energy, expected", testdata)
def test_scan(server, clients, scandir, scan_width, posX_s2, posY_s2, area_s2, area_s1, energy, expected):
    with clients(2) as (client1, client2):
        # Getting ID for ship 2, used to later to check if found
        resp = requests.post(client2.url + 'getShipInfo', json={
            'token': client2.token,
        })
        assert resp
        # Getting the id for ship 2
        resp_data = resp.json()
        client2_id = resp_data['id']

        # Set first ship to a centre position of 0,0
        resp = requests.post(client1.url + 'sudo', json={
            'token': client1.token,
            'posX': 0.0,
            'posY': 0.0,
            'area': area_s1,
            'energy': energy,
        })
        assert resp

        # Set second ship to desired location and setting its area via the test data
        resp2 = requests.post(client2.url + 'sudo', json={
            'token': client2.token,
            'posX': posX_s2,
            'posY': posY_s2,
            'area': area_s2,
        })
        assert resp2
        
        # Scanning from the first ship
        resp_scan = requests.post(client1.url + 'scan', json={
            'token': client1.token,
            'direction': scandir,
            'width': scan_width,
            'energy': energy,
        })
        assert resp_scan
        # Storing the results of scan
        scan_list = resp_scan.json()

       # Checking if the outcome matches the expected output
        found = any(scanned['id'] ==
                    client2_id for scanned in scan_list['scanned'])
        assert found == expected

# Performs a battle
def test_combat_battle(server, clients):
    reset_time(server)
    with clients(3) as (client1, client2, client3):
        # Setting up client 1
        resp = requests.post(client1.url + 'sudo', json={
            'token': client1.token,
            'posX': 0,
            'posY': 0,
            'energy': 200,
            'area': 20,
        })
        assert resp

        # Setting up client 2
        resp = requests.post(client2.url + 'sudo', json={
            'token': client2.token,
            'posX': 10,
            'posY': 0,
            'energy': 100,
            'area': 20,
        })
        assert resp

        set_time(server, 100000)

        # Setting up client 3
        resp = requests.post(client3.url + 'sudo', json={
            'token': client3.token,
            'posX': 30,
            'posY': 0,
            'energy': 10,
            'area': 5,
        })
        assert resp

        # Client 2 shooting client 3
        resp = requests.post(client2.url + 'shoot', json={
            'token': client2.token,
            'direction': 0,
            'width': 10,
            'energy': 50,
            'damage': 10,
        })
        assert resp

        # Client 1 shooting client 2
        resp = requests.post(client1.url + 'shoot', json={
            'token': client1.token,
            'direction': 0,
            'width': 10,
            'energy': 2,
            'damage': 100,
        })
        assert resp


        # Checking the client one gains both ship 2 and 3 area
        resp = requests.post(client1.url + 'getShipInfo', json={
            'token': client1.token,
        })

        resp_data = resp.json()
        assert resp_data['area'] == 45

# Scenario 1 test
def test_combat_scenario1(server, clients):
    # Scenario 1
    # Areas: C1 = 20 , C2 = 12 , C3 = 4
    # C1 shoots C2    (C2 area = area (12) - damage (3...))
    # C2 kill C3      (C2 new area = C2 area (8~) + C3 area (4) )
    # C1 kills C2     (C1 new area = C1 area + C2 area)  == ~32
    reset_time(server)
    with clients(3) as (client1, client2, client3):
        # Setting up client 1
        resp = requests.post(client1.url + 'sudo', json={
            'token': client1.token,
            'posX': 0,
            'posY': 30,
            'energy': 100000,
            'area': 20,
        })
        assert resp

        # Setting up client 2
        resp = requests.post(client2.url + 'sudo', json={
            'token': client2.token,
            'posX': 100,
            'posY': 30,
            'energy': 1000,
            'area': 12,
        })
        assert resp

        # Client 1 shooting client 2 with damage 0f 3~
        resp = requests.post(client1.url + 'shoot', json={
            'token': client1.token,
            'direction': 0,
            'width': 10,
            'energy': 10,
            'damage': 4,
        })
        assert resp

        # Setting up client 3
        resp = requests.post(client3.url + 'sudo', json={
            'token': client3.token,
            'posX': 150,
            'posY': 30,
            'area': 4,
        })
        assert resp

        # Client 2 shooting client 3 with damage 0f 4~ and kills client 3
        resp = requests.post(client2.url + 'shoot', json={
            'token': client2.token,
            'direction': 0,
            'width': 30,
            'energy': 40,
            'damage': 30,
        })
        assert resp

        # Checking the client 2 gains client 3 area
        resp = requests.post(client2.url + 'getShipInfo', json={
            'token': client2.token,
        })
        assert resp
        resp_data = resp.json()
        # Factoring in the previous loss of damage
        assert is_close(resp_data['area'], ((12 - 3.140369176864624) + 4))

        set_time(server, 10000)

        # Client 1 kills client 2 with a damage of 14~
        resp = requests.post(client1.url + 'shoot', json={
            'token': client1.token,
            'direction': 0,
            'width': 1,
            'energy': 15,
            'damage': 10,
        })
        assert resp

        # Checking the client 1 gains client 2 area
        resp = requests.post(client1.url + 'getShipInfo', json={
            'token': client1.token,
        })
        assert resp
        resp_data = resp.json()
        # From the calculations it should be > 32
        assert is_close(resp_data['area'], 32.859630823135376)

# Scenario 2 test
def test_combat_scenario2(server, clients):
    # Scenario 2
    # Areas: C1 = 10 , C2 = 10 , C3 = 3
    # C1 shoots C2    (C2 area = area (10) - damage (5...))
    # C2 kill C3      (C2 new area = C2 area (5~) + C3 area (3) )
    # C1 kills C2     (C1 new area = C1 area + C2 area)  == ~18
    reset_time(server)
    with clients(3) as (client1, client2, client3):
        # Setting up client 1
        resp = requests.post(client1.url + 'sudo', json={
            'token': client1.token,
            'posX': 0,
            'posY': 0,
            'energy': 100000,
            'area': 10,
        })
        assert resp

        # Setting up client 2
        resp = requests.post(client2.url + 'sudo', json={
            'token': client2.token,
            'posX': 300,
            'posY': 0,
            'energy': 1000,
            'area': 10,
        })
        assert resp

        # Client 3 is away from combat
        resp = requests.post(client3.url + 'sudo', json={
            'token': client3.token,
            'posX': 0,
            'posY': -1000,
            'area': 3,
        })
        assert resp

        # Client 1 shooting client 2 with damage 0f 5~
        resp = requests.post(client1.url + 'shoot', json={
            'token': client1.token,
            'direction': 0,
            'width': 5,
            'energy': 100,
            'damage': 1,
        })
        assert resp

        # Setting up client 3 in postion
        resp = requests.post(client3.url + 'sudo', json={
            'token': client3.token,
            'posX': 330,
            'posY': 0,

        })
        assert resp

        # Client 2 shooting client 3 with damage 0f 5~ and kills client 3
        resp = requests.post(client2.url + 'shoot', json={
            'token': client2.token,
            'direction': 0,
            'width': 10,
            'energy': 5,
            'damage': 10,
        })
        assert resp

        set_time(server, 10000)

        # Checking that client 2 gains client 3 area
        resp = requests.post(client2.url + 'getShipInfo', json={
            'token': client2.token,
        })

        resp_data = resp.json()
        # Factoring in the previous loss of damage
        assert is_close(resp.json()['area'], ((10 - 5.115637302398682) + 3) )

        # Moving client 1 closer for the kill shot
        resp = requests.post(client1.url + 'sudo', json={
            'token': client1.token,
            'posX': 250,
            'posY': 0,
        })
        assert resp

        # Client 1 kills client 2 with a damage of 10~
        resp = requests.post(client1.url + 'shoot', json={
            'token': client1.token,
            'direction': 0,
            'width': 0.5,
            'energy': 80,
            'damage': 4.5,
        })
        assert resp

        # Checking client 1 gains client 2's area
        resp = requests.post(client1.url + 'getShipInfo', json={
            'token': client1.token,
        })
        assert resp
        resp_data = resp.json()
        # Factoring in the previous loss of damage
        assert resp_data['area'] == 20  # This should be 20


# Testing combat cooldown
def test_cool_down(server, clients):
    reset_time(server)
    with clients(3) as (client1, client2, client3):
        # Setting up client 1
        resp = requests.post(client1.url + 'sudo', json={
            'token': client1.token,
            'posX': 0,
            'posY': 0,
            'energy': 10000,
            'area': 50,
        })
        assert resp

        # Setting up client 2
        resp = requests.post(client2.url + 'sudo', json={
            'token': client2.token,
            'posX': 15,
            'posY': 0,
            'energy': 1000,
            'area': 25,
        })
        assert resp

        # Setting up client 3
        resp = requests.post(client3.url + 'sudo', json={
            'token': client3.token,
            'posX': -15,
            'posY': 0,
            'energy': 1000,
            'area': 20,
        })
        assert resp

        # Client 1 shoots client 2
        resp = requests.post(client1.url + 'shoot', json={
            'token': client1.token,
            'direction': 0,
            'width': 10,
            'energy': 5,
            'damage': 10,
        })
        assert resp

        # Getting client 2's damaged area
        resp = requests.post(client2.url + 'getShipInfo', json={
            'token': client2.token,
        })

        resp_data = resp.json()
        client2_new_area = resp_data['area']

        # Client 1 shoots client 3
        resp = requests.post(client1.url + 'shoot', json={
            'token': client1.token,
            'direction': 180,
            'width': 10,
            'energy': 5,
            'damage': 10,
        })
        assert resp

        # Checking client 1 has initial area
        resp = requests.post(client1.url + 'getShipInfo', json={
            'token': client1.token,
        })

        resp_data = resp.json()
        assert resp_data['area'] == 50

        # Client 1 kills client 3
        resp = requests.post(client1.url + 'shoot', json={
            'token': client1.token,
            'direction': 180,
            'width': 10,
            'energy': 5,
            'damage': 10,
        })
        assert resp

        # Getting client 1 area
        resp = requests.post(client1.url + 'getShipInfo', json={
            'token': client1.token,
        })
        assert resp
        resp_data = resp.json()
        client1_area_after_kill = resp_data['area']

        # Checking client 1 gained client 3 area
        resp = requests.post(client1.url + 'getShipInfo', json={
            'token': client1.token,
        })
        assert resp
        resp_data = resp.json()
        assert resp_data['area'] == 70

        set_time(server, 100000)

        # Client 1 kills client 2
        resp = requests.post(client1.url + 'shoot', json={
            'token': client1.token,
            'direction': 0,
            'width': 10,
            'energy': 15,
            'damage': 15,
        })
        assert resp

        # Client 1 gains the new of client 2
        resp = requests.post(client1.url + 'getShipInfo', json={
            'token': client1.token,
        })
        assert resp
        resp_data = resp.json()
        assert is_close(resp_data['area'], client1_area_after_kill + client2_new_area)

def test_shield_uses_energy(server, clients):
    reset_time(server)
    with clients(2) as (client1,client2):
        
        # get ship's energy
        energy_shield = requests.post(client1.url + 'getShipInfo', json = {
            'token' : client1.token,
        }).json()['energy']

        # get other ship's energy
        energy_noshield = requests.post(client2.url + 'getShipInfo', json = {
            'token' : client2.token,
        }).json()['energy']

        # set up a 45(x2) degree shield
        resp = requests.post(client1.url + 'shield', json = {
            'token' : client1.token,
            'direction' : 0,
            'width' : 45,
        })
        assert resp 

        # use a bunch of energy for both ships
        resp = requests.post(client1.url + 'scan', json = {
            'token' : client1.token,
            'direction' : 0,
            'width' : 45,
            'energy' : 5
        })
        assert resp

        resp = requests.post(client2.url + 'scan', json = {
            'token' : client2.token,
            'direction' : 0,
            'width' : 45,
            'energy' : 5
        })
        assert resp

        # wait two seconds
        set_time(server, 2000)

        # check that shielded ship has (considerably) lower energy than unshielded
        energy_shield2 = requests.post(client1.url + 'getShipInfo', json = {
            'token' : client1.token,
        }).json()['energy']

        energy_noshield2 = requests.post(client2.url + 'getShipInfo', json = {
            'token' : client2.token,
        }).json()['energy']

        # subtract 0.01 from energy_noshield2 to ensure that the difference is not rounding
        assert energy_shield2 < energy_noshield2 - 0.01

shield_shipInfo_data = [
    (0,0),
    (12, 30),
    (47, 180),
    (91, -180),
    (179, 4000),
    (180, -360),
    (12.345, 411.91),
    (0.01, -2700),
    (179.9987, 3600)
]

@pytest.mark.parametrize('width,direction', shield_shipInfo_data)
def test_shield_shipInfo(server, clients, width, direction):

    reset_time(server)
    with clients(1) as (client1):

        assert requests.post(client1.url + 'shield', json = {
            'token' : client1.token,
            'width' : width,
            'direction' : direction,
        })

        data = requests.post(client1.url + 'getShipInfo', json = {
            'token' : client1.token,
            }).json()

        assert is_close(data['shieldWidth'], width)
        assert is_close( (direction-data['shieldDir']) % 360, 0)
        assert 0 <= data['shieldDir'] and data['shieldDir'] < 360


# area energy width(deg) time(s) expected
shield_energy_usage_data = [
    # have no shield, use no energy
    (47, 0, 0, 6.5, 6.5*47),
    # have half of a shield, stay energy neutral
    (31, 12, 90, 123, 12),
    (1234.567, 6432.21, 90, 1223, 6432.21),
    # even at almost no energy
    (456.789, 0.0001, 90, 1223, 0.0001),
    # have a full shield, go out of energy in area seconds
    (25, 250, 180, 24.999, 0),
    (50, 250, 180, 24.999, 0),
    (23.5, 235, 180, 23.499, 0),

    # random data for our precise function currently used
    (123, 300, 69, 4, 300 + 123*4 - 4*94.3),
    (234, 345, 125, 6, 345 + 234*6 - 234*6 - 3.88888 * 6)
]

# tests if the shields use energy as expected
@pytest.mark.parametrize('area, energy, width, time, expected', shield_energy_usage_data)
def test_shield_energy_usage(server, clients, area, energy, width, time, expected):
    reset_time(server)
    with clients(1) as (client1):

        assert requests.post(client1.url + 'sudo', json = {
            'token' : client1.token,
            'area' : area,
            'energy': energy,
        })

        assert requests.post(client1.url + 'shield', json = {
            'token' : client1.token,
            'direction' : 0,
            'width' : width,
        })


        set_time(server,time*1000)

        resp = requests.post(client1.url + 'getShipInfo', json = {
            'token' : client1.token,
        })

        assert resp 

        data = resp.json()

        assert is_close(data['energy'],expected,0.02)
        assert is_close(data['shieldWidth'], width)

shield_overuse_data = [
    # use a huge shield and wait a long time - should be back at full energy
    (200, 1800, 180, 500, 2000),
    # try to use a shield at 0 energy with greater width than 90 - should turn off instantly
    (200, 0, 90.01, 5, 1000),
    # use up 1 extra energy per second - die after 50, regenerate energy for 2.5 seconds
    (300, 50, 99, 52.5, 750),
    # use up 0.3333 energy per second - die after 24, regenerate energy for 1 second
    (876, 8, 93, 25, 876)

]

# tests if the shields turn off and allow energy to recover
@pytest.mark.parametrize('area, energy, width, time, expected', shield_overuse_data)
def test_shield_overuse(server, clients, area, energy, width, time, expected):
    reset_time(server)
    with clients(1) as (client1):

        assert requests.post(client1.url + 'sudo', json = {
            'token' : client1.token,
            'area' : area,
            'energy': energy,
        })

        assert requests.post(client1.url + 'shield', json = {
            'token' : client1.token,
            'direction' : 0,
            'width' : width,
        })


        set_time(server,time*1000)

        resp = requests.post(client1.url + 'getShipInfo', json = {
            'token' : client1.token,
        })

        assert resp 

        data = resp.json()

        assert is_close(data['energy'],expected,0.02)
        assert is_close(data['shieldWidth'], 0)

# data for test_shield_miss
# shooter shielder shield shot expected
# shooter: sudo json to set up client1
# shielder: sudo json to set up client2
# shield: shield json to activate shield
# shoot: shoot json to shoot
shield_miss_data = [

    # broad hit to the right, with the shield being a 90 degree cone on the right
    ( {'posX':0,'posY':0,'area':30,'energy':300}, {'posX':50, 'posY':0, 'area':30, 'energy':300}, {'direction':0,'width':45},{'direction':0,'width':30,'energy':10,'damage':1} ),
    # similar, but now shooting downwards, and shield is downwards
    ( {'posX':0,'posY':100,'area':30,'energy':300}, {'posX':0, 'posY':17, 'area':30, 'energy':300}, {'direction':-90,'width':45},{'direction':-90,'width':30,'energy':100,'damage':1} ),
    # now up
    ( {'posX':-50,'posY':-50,'area':30,'energy':300}, {'posX':-50, 'posY':50, 'area':30, 'energy':300}, {'direction':-270,'width':45},{'direction':-270,'width':30,'energy':200,'damage':1} ),
    # now left
    ( {'posX':-50,'posY':-50,'area':30,'energy':300}, {'posX':-100, 'posY':-50, 'area':30, 'energy':300}, {'direction':180,'width':45},{'direction':180,'width':30,'energy':200,'damage':1} ),
    
    # TC5
    ( {'posX':5,'posY':-5,'area':50,'energy':500}, {'posX':-9, 'posY':0, 'area': 113.0973,'energy':1000}, {'direction':180, 'width': 150}, {'direction': 148, 'width':10, 'energy':50,'damage':1} ),

    # TC6
    ( {'posX':-3,'posY':1,'area':3.1415,'energy':30}, {'posX':-5,'posY':4,'area':12.5664,'energy':120}, {'direction':149.1,'width':149}, {'direction':71.6,'width':26.6, 'energy':15, 'damage':2} ),

    # same as TC6 but horrible directions
    ( {'posX':-3,'posY':1,'area':3.1415,'energy':30}, {'posX':-5,'posY':4,'area':12.5664,'energy':120}, {'direction':149.1-360*47,'width':149}, {'direction':71.6+360*9,'width':26.6, 'energy':15, 'damage':2} ),

    # TC8 shielding [I,J] (that's a half of shield)
    ( {'posX':-6,'posY':-4,'area':28.27433,'energy':280}, {'posX':-5,'posY':3,'area':50.265482,'energy':500}, {'direction':-29.7,'width':69.5}, {'direction':102.8,'width':12.959, 'energy':47, 'damage':1.3} ),

    # TC8 shielding (E,K) (that's a half of shield)
    ( {'posX':-6,'posY':-4,'area':28.27433,'energy':280}, {'posX':-5,'posY':3,'area':50.265482,'energy':500}, {'direction':75.6,'width':134.4}, {'direction':102.8,'width':12.959, 'energy':47, 'damage':1.3} ),

    # TC9 
    ( {'posX':0,'posY':0,'area':3.1415926535,'energy':10}, {'posX':44, 'posY':0, 'area':3.1415926535, 'energy':10}, {'direction':0,'width':107},{'direction':0,'width':30,'energy':1,'damage':1} ),
      
]

# this test gets scenarios where an activated shield is completely missed by a shot
# and so the damage of a shot should sbe unchanged on activation
@pytest.mark.parametrize('shooter, shielder, shield, shoot', shield_miss_data)
def test_shield_miss(server, clients, shooter, shielder, shield, shoot):
    reset_time(server)
    with clients(2) as (attacker, victim):

        shooter['token'] = attacker.token
        shoot['token'] = attacker.token
        shielder['token'] = victim.token
        shield['token'] = victim.token
        
        resp = requests.post(attacker.url + 'sudo',json=shooter)
        assert resp 

        resp = requests.post(victim.url + 'sudo', json=shielder)
        assert resp 

        # shoot, ensure that victim was struck
        resp = requests.post(attacker.url + 'shoot',json=shoot)
        print(resp.json())
        assert resp 
        data = resp.json()
        assert len(data['struck']) == 1

        # store damage dealt
        resp = requests.post(victim.url + 'getShipInfo', json={
            'token' : victim.token
        })
        assert resp 
        damage_no_shield = shielder['area'] - resp.json()['area']

        # reset both ships
        resp = requests.post(attacker.url + 'sudo',json=shooter)
        assert resp 

        resp = requests.post(victim.url + 'sudo', json=shielder)
        assert resp

        # activate shield
        resp = requests.post(victim.url + 'shield',json=shield)
        assert resp

        # shoot again
        resp = requests.post(attacker.url + 'shoot',json=shoot)
        assert resp 
        data = resp.json()
        assert len(data['struck']) == 1

        # look at damage dealt this time
        resp = requests.post(victim.url + 'getShipInfo', json={
            'token' : victim.token
        })
        assert resp 
        damage_shield = shielder['area'] - resp.json()['area']

        # damage should be the same
        assert is_close(damage_shield, damage_no_shield)

# data for test_shield_full
# shooter shielder shield shot expected
# shooter: sudo json to set up client1
# shielder: sudo json to set up client2
# shield: shield json to activate shield
# shoot: shoot json to shoot
shield_full_data = [

    #shield everything (half-angle 180)
    ( {'posX':0,'posY':0,'area':30,'energy':300}, {'posX':50, 'posY':0, 'area':30, 'energy':300}, {'direction':180,'width':180},{'direction':0,'width':30,'energy':10,'damage':1} ),
    # broad hit to the right, with the shield being a half-circle on the left
    ( {'posX':0,'posY':0,'area':30,'energy':300}, {'posX':50, 'posY':0, 'area':30, 'energy':300}, {'direction':180,'width':90},{'direction':0,'width':30,'energy':10,'damage':1} ),
    # similar, but now shooting downwards, and shield is upwards
    ( {'posX':0,'posY':100,'area':30,'energy':300}, {'posX':0, 'posY':17, 'area':30, 'energy':300}, {'direction':90,'width':90},{'direction':-90,'width':30,'energy':100,'damage':1} ),
    # now up
    ( {'posX':-50,'posY':-50,'area':30,'energy':300}, {'posX':-50, 'posY':50, 'area':30, 'energy':300}, {'direction':270,'width':90},{'direction':-270,'width':30,'energy':200,'damage':1} ),
    # now left
    ( {'posX':-50,'posY':-50,'area':30,'energy':300}, {'posX':-100, 'posY':-50, 'area':30, 'energy':300}, {'direction':0,'width':90},{'direction':180,'width':30,'energy':200,'damage':1} ),

    # only get hit by the circular part of shot, which is shielded fully (inverse shield of TC9)
    ( {'posX':0,'posY':0,'area':3.14159265,'energy':10}, {'posX':44, 'posY':0, 'area':3.1415926535, 'energy':10}, {'direction':180,'width':73},{'direction':0,'width':30,'energy':1,'damage':1} ),
    
    #TC10
    ( {'posX':-1,'posY':4,'area':12.5663706,'energy':10}, {'posX':2,'posY':-1,'area':12.5663706,'energy':10}, {'direction':120.3,'width':30},{'direction':-58.706,'width':13.6,'energy':1,'damage':1} )
]

# this test gets scenarios where an shield is fully blocked by an activated shield
# and so the damage of a shot should be 0 on activation
@pytest.mark.parametrize('shooter, shielder, shield, shoot', shield_full_data)
def test_shield_full(server, clients, shooter, shielder, shield, shoot):
    reset_time(server)
    with clients(2) as (attacker, victim):

        shooter['token'] = attacker.token
        shoot['token'] = attacker.token
        shielder['token'] = victim.token
        shield['token'] = victim.token
        
        resp = requests.post(attacker.url + 'sudo',json=shooter)
        assert resp 

        resp = requests.post(victim.url + 'sudo', json=shielder)
        assert resp 

        # shoot, ensure that victim was struck
        resp = requests.post(attacker.url + 'shoot',json=shoot)
        assert resp 
        data = resp.json()
        assert len(data['struck']) == 1

        # store damage dealt
        resp = requests.post(victim.url + 'getShipInfo', json={
            'token' : victim.token
        })
        assert resp 
        damage_no_shield = shielder['area'] - resp.json()['area']

        assert damage_no_shield > 0

        # reset both ships
        resp = requests.post(attacker.url + 'sudo',json=shooter)
        assert resp 

        resp = requests.post(victim.url + 'sudo', json=shielder)
        assert resp

        # activate shield
        resp = requests.post(victim.url + 'shield',json=shield)
        assert resp

        # shoot again
        resp = requests.post(attacker.url + 'shoot',json=shoot)
        assert resp 
        data = resp.json()
        assert len(data['struck']) == 1

        # look at damage dealt this time
        resp = requests.post(victim.url + 'getShipInfo', json={
            'token' : victim.token
        })
        assert resp 
        damage_shield = shielder['area'] - resp.json()['area']

        # damage should be zero
        assert damage_shield == 0


# data for test_shield_full
# shooter shielder shield shot block
# shooter: sudo json to set up client1
# shielder: sudo json to set up client2
# shield: shield json to activate shield
# shoot: shoot json to shoot
# block: portion of damage blocked
shield_partial_data = [

    #TC11
    ( {'posX':-1,'posY':4,'area':12.5663706,'energy':10}, {'posX':2,'posY':-1,'area':12.5663706,'energy':10}, {'direction':105.16559,'width':15.16559},{'direction':-58.706,'width':13.6,'energy':1,'damage':1}, 0.5 ),

    #TC12 (shielding more to the right so shield is all within shot)
    ( {'posX':3,'posY':4,'area':3.141592,'energy':10}, {'posX':-1,'posY':2,'area':28.2743338823 ,'energy':10}, {'direction':38.029,'width':22.5},{'direction':-155.3,'width':38.735,'energy':1,'damage':1}, 1-45/61.92751 ),
    
    #TC12 but shielding at I
    ( {'posX':3,'posY':4,'area':3.141592,'energy':10}, {'posX':-1,'posY':2,'area':28.2743338823 ,'energy':10}, {'direction':48.029,'width':22.5},{'direction':-155.3,'width':38.735,'energy':1,'damage':1}, 1-36.39873/61.92751 ),
    

]

# this test gets scenarios where an shield is fully blocked by an activated shield
# and so the damage of a shot should be 0 on activation
@pytest.mark.parametrize('shooter, shielder, shield, shoot, block', shield_partial_data)
def test_shield_partial(server, clients, shooter, shielder, shield, shoot, block):
    reset_time(server)
    with clients(2) as (attacker, victim):

        shooter['token'] = attacker.token
        shoot['token'] = attacker.token
        shielder['token'] = victim.token
        shield['token'] = victim.token
        
        resp = requests.post(attacker.url + 'sudo',json=shooter)
        assert resp 

        resp = requests.post(victim.url + 'sudo', json=shielder)
        assert resp 

        # shoot, ensure that victim was struck
        resp = requests.post(attacker.url + 'shoot',json=shoot)
        assert resp 
        data = resp.json()
        assert len(data['struck']) == 1

        # store damage dealt
        resp = requests.post(victim.url + 'getShipInfo', json={
            'token' : victim.token
        })
        assert resp 
        damage_no_shield = shielder['area'] - resp.json()['area']

        assert damage_no_shield > 0

        # reset both ships
        resp = requests.post(attacker.url + 'sudo',json=shooter)
        assert resp 

        resp = requests.post(victim.url + 'sudo', json=shielder)
        assert resp

        # activate shield
        resp = requests.post(victim.url + 'shield',json=shield)
        assert resp

        # shoot again
        resp = requests.post(attacker.url + 'shoot',json=shoot)
        assert resp 
        data = resp.json()
        assert len(data['struck']) == 1

        # look at damage dealt this time
        resp = requests.post(victim.url + 'getShipInfo', json={
            'token' : victim.token
        })
        assert resp 
        damage_shield = shielder['area'] - resp.json()['area']

        # damage should be block times as before
        assert is_close(damage_shield / damage_no_shield, block, 0.01)
