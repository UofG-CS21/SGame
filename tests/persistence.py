import requests
import pytest
import time

def test_persist_ships(persistence, server, clients):
    """
    Tests that ships are persisted to ElasticSearch correctly.
    """
    if not persistence:
        pytest.skip("--persistence tests not enabled by user")

    ships_info = {}

    with clients(4) as ships:
        for ship in ships:
            resp = requests.post(ship.url + 'getShipInfo', json={
                'token': ship.token,
            })
            assert resp

            info = resp.json()
            assert 'id' in info
            ships_info[ship.token] = info

    time.sleep(1)

    for token, info in ships_info.items():
        print('getShipInfo(' + token + '): ', info)
        elastic_url = persistence.url + 'ships/_doc/' + token
        print('GET', elastic_url)
        resp = requests.get(elastic_url)
        assert resp

        elastic_json = resp.json()
        print(elastic_json)
        assert '_source' in elastic_json
        for key in ['energy', 'area', 'posX', 'posY', 'shieldDir', 'shieldWidth']:
            assert elastic_json['_source'][key] == info[key]
