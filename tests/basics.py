import requests


def test_disconnect(client):
    """
    Tests that a connected ship can disconnect via REST.
    """
    resp = requests.post(client.url + 'disconnect',
                         json={'token': client.token})
    assert resp


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

    # Test values
    x1 = 23.6
    y1 = 45.5
    x2 = 55.5
    y2 = 12.3
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
    assert resp_data['velX'] == sumX
    assert resp_data['velY'] == sumY

    # Disconnect
    resp = requests.post(client.url + 'disconnect',
                         json={'token': client.token})
    assert resp
