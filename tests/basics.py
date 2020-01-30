import requests
import time
import pytest

allowed_fpe = 1e-6


def isClose(a, b, err=allowed_fpe):
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
        assert resp_data['posX'] == 0
        assert resp_data['posY'] == 0
        assert resp_data['velX'] == 0
        assert resp_data['velY'] == 0


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
        assert resp_data['posX'] == 0
        assert resp_data['posY'] == 0
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
        assert isClose(resp_data['velX'], sumX)
        assert isClose(resp_data['velY'], sumY)

        # Wait for energy to recharge
        set_time(server, 10000)

        resp = requests.post(server.url + 'getShipInfo', json={
            'token': client.token,
        })
        assert isClose(resp.json()['energy'], 10)

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
        assert isClose(resp_data['velX'], 0)
        assert isClose(resp_data['velY'], 0)

        # Wait for energy to recover
        set_time(server, 20000)

        resp = requests.post(server.url + 'getShipInfo', json={
            'token': client.token,
        })
        assert isClose(resp.json()['energy'], 10)

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
        assert isClose(resp_data['energy'], 0, 0.01)
        assert isClose(resp_data['velX'], 9.0)
        assert isClose(resp_data['velY'], -1.0)

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
        assert isClose(resp_data['posX'], old_x + 9.0 * 4.5, 0.5)
        assert isClose(resp_data['posY'], old_y + (-1.0) * 4.5, 0.5)

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
            assert not isClose(v, init_state[k], 0.001)

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
            assert isClose(v, kvs[k], 0.001)


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
        assert client1_energy <= 6 # calculation below!
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
        assert client2_area == (client2_area_before - 2.685387372970581)

# Dataset for death test 
test_death_data = [
    # FORMAT: client1 posX, client1 posY, client1 area, client1 energy, client2 posX, client2 posY, client2 area ,shoot dir ,shoot width ,shoot energy, damage scaling
    (0,0,25,50,5,5,5,0,45,15,10),
    (2.5,3,30,50,6,4,8,0,30,15,9),
    (4,4,40,40,8,4,4,0,25,20,8),
    (100,0,50,100,-50,0,5,180,20,70,10),
]
@pytest.mark.parametrize("client1_x, client1_y, client1_area, client1_energy, client2_x, client2_y, client2_area, direction, width, energy , damage", test_death_data)
def test_combat_death(server, clients,client1_x, client1_y, client1_area, client1_energy, client2_x, client2_y, client2_area, direction, width, energy , damage):
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
            'area': area_s1,
            'energy': energy
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
        
        resp_data = resp.json()
        # Factoring in the previous loss of damage
        assert resp_data['area'] == ((12 - 3.140369176864624) + 4)

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
        
        resp_data = resp.json()
        # From the calculations it should be 32~
        assert resp_data['area'] == 32.859630823135376

