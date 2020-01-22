import requests
import time
import pytest


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

        # Waiting
        time.sleep(1)

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


allowed_fpe = 1e-6


def isClose(a, b, err=allowed_fpe):
    return abs(a-b) <= err


def test_movement(server, clients):
    """
    Tests that accelerate/ movement such that a ship can accelerate using an x and y.
    """
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
        time.sleep(10)
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
        time.sleep(10)
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
        time.sleep(4.5)

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


def test_sudo_fail(server):
    """
    Tests that the "sudo" endpoint fails if no valid token is passed.
    """
    resp = requests.post(server.url + 'sudo',
                         json={'token': '**NOT_A_VALID_TOKEN**'})
    assert not resp

    resp = requests.post(server.url + 'sudo',
                         json={})
    assert not resp

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
    (0, 45, 850, 0, 6 ,103, 1000, True),

    # Case 5: Ship outwith circular segment
    (0, 45, 1500, 0, 10, 100, 1000, False),

    # Case 6: Ship ouside of the scan
    (0, 15, 900, 0, 10, 100, 1000, True),

    # Case 7: Ship on upper boundary scan
    (0, 15, 1887.8151, 505.838353, 10, 100, 1000, True),

    # Case 8: Ship on lower boundary scan
    (0, 15, 1887.8151, -505.838353, 10, 100, 1000, True),

    # Case 9: Ship is on the end boundry of the scan
    (0, 45, 900, 0, 0, 900, 1000, True),

    # Case 10: Ship is on the circular section
    (0, 45, 90, 0, 10, 90, 1000, True),

    # Case 11: Ship is behind the scan area
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
        resp = requests.post(client1.url + 'sudo', json = {
            'token': client1.token,
            'posX': 0,
            'posY': 0,
            'area': area_s1,
            'energy' : energy
        })
        assert resp

        resp = requests.post(client1.url + 'getShipInfo', json = {
                'token': client1.token
            })
        #print(resp.text)

        # Set second ship to desired location and setting its area via the test data
        resp2 = requests.post(client2.url + 'sudo', json = {
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


