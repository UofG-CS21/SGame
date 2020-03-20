import requests
import argparse
import time

parser = argparse.ArgumentParser()
parser.add_argument('-P', '--port', type=int, default = 5000, help='Port to connect to')
parser.add_argument('-H', '--host', type=str, default = 'localhost', help = 'Host to connect to')

args = parser.parse_args()
port = getattr(args,'port')
host = getattr(args,'host')

def APICall(token, API, data = {}):
    data['token'] = token
    response = requests.post(url='http://' + host + ':' + str(port) + '/' + API,json = data)
    return response

#Two connections, A and B connect to the server. Session tokens are stored to allow control of Ship A and Ship B on the server.
print('Sending connect request A...')
response = requests.post(url='http://' + host + ':' + str(port) + '/connect')
Atoken = response.json()['token']
print('Received session token ' + Atoken)

print('Sending connect request B...')
response = requests.post(url='http://' + host + ':' + str(port) + '/connect')
Btoken = response.json()['token']
print('Received session token ' + Btoken)

print('Placing B 500 units right')
response = APICall(Btoken, 'sudo', {'posX' : 500})
print('B is now:' + APICall(Btoken, 'getShipInfo').text)

print('Shooting B with angle 30 (too far)')
response = APICall(Atoken, 'shoot', {'direction' : 0, 'width' : 30, 'energy' : 160, 'damage' : 0.0625})
print('Shot: ' + response.text)

#refuel A
APICall(Atoken,'sudo',{'energy':1000})

print('Shooting B with angle 30 (non-lethal)')
response = APICall(Atoken, 'shoot', {'direction' : 0, 'width' : 30, 'energy' : 300, 'damage' : 0.033})
print('Shot: ' + response.text)

print('B is now: ' + APICall(Btoken, 'getShipInfo').text)

#do it again, killing B
APICall(Atoken,'sudo',{'energy':1000})

print('Shooting B with angle 30 (enough damage)')
response = APICall(Atoken, 'shoot', {'direction' : 0, 'width' : 30, 'energy' : 300, 'damage' : 0.033})
print('Shot: ' + response.text)

print('B is now: ' + APICall(Btoken, 'getShipInfo').text)
print('A is now: ' + APICall(Atoken, 'getShipInfo').text)

#Disconnect the ships.
print('Sending disconnect request A...')
response = APICall(Atoken, 'disconnect')
print('Received response: ' + response.text)

print('Sending disconnect request B...')
response = APICall(Btoken, 'disconnect')
print('Received response: ' + response.text)
