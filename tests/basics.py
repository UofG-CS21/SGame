import requests
import time

def test_disconnect(client):
    """
    Tests that a connected ship can disconnect via REST.
    """
    resp = requests.post(client.url + 'disconnect',
                         json={'token': client.token})
    assert resp

allowed_fpe = 1e-6

def isClose(a, b, err = allowed_fpe):
    return abs(a-b) <= err

def test_movement(client):
    """
    Tests that accelerate/ movement such that a ship can accelerate using an x and y.
    """

    # Getting the intial ship info
    resp = requests.post(client.url + 'getShipInfo', json={
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
    resp = requests.post(client.url + 'accelerate', json={
        'token': client.token,
        'x': x1,
        'y': y1,
    })
    assert resp

    # Calling accelerate with second values
    resp = requests.post(client.url + 'accelerate', json={
        'token': client.token,
        'x': x2,
        'y': y2,
    })
    assert resp

    # Using getShipInfo to check if the values match the expected result
    resp = requests.post(client.url + 'getShipInfo', json={
        'token': client.token,
    })
    assert resp

    # Checking the values are correct
    resp_data = resp.json()
    assert isClose(resp_data['velX'],sumX)
    assert isClose(resp_data['velY'],sumY)

    # Wait for energy to recharge
    time.sleep(10)
    resp = requests.post(client.url + 'getShipInfo', json={
        'token': client.token,
    })
    assert isClose(resp.json()['energy'],10)

    # Accelerate in the opposite direction
    resp = requests.post(client.url + 'accelerate', json={
        'token': client.token,
        'x': -x1,
        'y': -y1,
    })
    assert resp

    resp = requests.post(client.url + 'accelerate', json={
        'token': client.token,
        'x': -x2,
        'y': -y2,
    })
    assert resp

    # Using getShipInfo to check if the values match the expected result
    resp = requests.post(client.url + 'getShipInfo', json={
        'token': client.token,
    })
    assert resp

    resp_data = resp.json()
    assert isClose(resp_data['velX'],0)
    assert isClose(resp_data['velY'],0)

    # Wait for energy to recover
    time.sleep(10)
    resp = requests.post(client.url + 'getShipInfo', json={
        'token': client.token,
    })
    assert isClose(resp.json()['energy'],10)

    # Accelerate requiring too much energy
    X = 90.0
    Y = -10.0

    resp = requests.post(client.url + 'accelerate', json={
        'token': client.token,
        'x': X,
        'y': Y,
    })
    assert resp

    # Check that we used up all energy and accelerated a proportion of what we asked for
    resp = requests.post(client.url + 'getShipInfo', json={
        'token': client.token,
    })
    assert resp

    resp_data = resp.json()
    assert isClose(resp_data['energy'],0,0.01)
    assert isClose(resp_data['velX'],9.0)
    assert isClose(resp_data['velY'],-1.0)

    old_x = resp_data['posX']
    old_y = resp_data['posY']

    # give the ship time to move
    time.sleep(4.5)

    # check that it moved the correct amount
    resp = requests.post(client.url + 'getShipInfo', json={
        'token': client.token,
    })
    assert resp

    resp_data = resp.json()
    assert isClose(resp_data['posX'], old_x + 9.0 * 4.5, 0.5)
    assert isClose(resp_data['posY'], old_y + (-1.0) * 4.5, 0.5)

    # Disconnect
    resp = requests.post(client.url + 'disconnect',
                         json={'token': client.token})
    assert resp
