import requests
import argparse
import time

parser = argparse.ArgumentParser()
parser.add_argument('-P', '--port', type=int, default = 5000, help='Port to connect to')
parser.add_argument('-H', '--host', type=str, default = 'localhost', help = 'Host to connect to')

args = parser.parse_args()
port = getattr(args,'port')
host = getattr(args,'host')

print('Sending connect request...')
response = requests.post(url='http://' + host + ':' + str(port) + '/connect')
token = response.json()['token']
print('Received session token ' + token)

data = {'token' : token, 'x' : 1, 'y' : 2}
print('Accelerating...')
response = requests.post(url = 'http://' + host + ':' + str(port) + '/accelerate', json = data)


print('Waiting a second...')
time.sleep(1)

data = {'token' : token}
print('Getting state')
response = requests.post(url = 'http://' + host + ':' + str(port) + '/getShipInfo', json = data)
print(response.json())

data = {'token' : token}
print('Sending disconnect request...')
response = requests.post(url = 'http://' + host + ':' + str(port) + '/disconnect', json = data)
print('Received response: ' + response.text)
