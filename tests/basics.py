import requests
import time

allowed_fpe = 1e-6


def isClose(a, b, err=allowed_fpe):
    return abs(a-b) <= err

# call at the BEGIiNNING of a test if you want it to use manual time. Time will be set to 0.
def reset_time(server, token):
    # set time to 0
    resp = requests.post(server.url + 'sudo', json={
        'token' : token,
        'time' : 0,
    })
    assert resp

    #force update the ship (which will mess up since it went back in time)
    resp = requests.post(server.url + 'getShipInfo', json={
        'token' : token
    })


    energy_cap = resp.json()['area'] * 10
    resp = requests.post(server.url + 'sudo', json={
        'token' : token,
        'energy' : energy_cap
    })
    assert resp

def set_time(server, token, time):
    resp = requests.post(server.url + 'sudo', json={
        'token' : token,
        'time' : time,
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
    with clients(2) as (client1, client2):

        reset_time(server, client1.token)

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
        set_time(server, client1.token, 1000)

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
    with clients(1) as client:
        # Getting the intial ship info
        resp = requests.post(server.url + 'getShipInfo', json={
            'token': client.token,
        })
        assert resp

        reset_time(server, client.token)

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
        resp = requests.post(server.url + 'sudo', json={
            'token': client.token,
            'time': 10000,
            })
        assert resp


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
        resp = requests.post(server.url + 'sudo', json={
            'token': client.token,
            'time': 20000,
            })
        assert resp

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
        resp = requests.post(server.url + 'sudo', json={
            'token': client.token,
            'time': 24500,
            })
        assert resp

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
