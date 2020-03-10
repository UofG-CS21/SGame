import requests
import time
import abc
import random
from multiprocessing import Process, Pipe

# functions to generate random json data for API requests

def GSIgen():
    return {}

def accgen():
    return {'x': random.randrange(-5,5), 'y':random.randrange(-5,5)}

def scangen():
    return {'width':random.randrange(1,90),'direction':random.randrange(-345,456),'energy':random.randrange(1,10)}

def shootgen():
    return {'width':random.randrange(1,90),'direction':random.randrange(-345,456),'energy':random.randrange(1,10),'damage':0.000001}

def shieldgen():
    return {'width':random.randrange(1,180),'direction':random.randrange(-345,456)}

API = [ ['shoot', shootgen], ['scan', scangen], ['getShipInfo', GSIgen], ['accelerate', accgen], ['shield', shieldgen] ]

def randomAPI():
    api = random.choice(API)
    return api[0], api[1]()

class Bot:

    def __init__(self,server):
        self.host = server.host
        self.port = server.port
        resp = requests.post(url='http://' + self.host + ':' + str(self.port) + '/connect')
        self.token = resp.json()['token']
        self.apiCallTimes = {}
        self.p = None
        self.pipe = None
        self.name = "Bot"

    #def api(self, path, data):
    #    data['token'] = self.token
    #    return requests.post(url = 'http://' + self.host + ':' + str(port) + '/' + path, json=data)

    def quit(self):
        data = { 'token': self.token }
        return requests.post(url = 'http://' + self.host + ':' + str(self.port) + '/disconnect', json=data)

    def timedapi(self, path, data = {}):
        data['token'] = self.token
        start = time.time()
        resp = requests.post(url = 'http://' + self.host + ':' + str(self.port) + '/' + path, json=data)
        end = time.time()
        if path not in self.apiCallTimes:
            self.apiCallTimes[path] = []
        self.apiCallTimes[path].append(end-start)
        return (resp,end-start)

    def generate_output(self):
        return 1

    def run_inner(self, output, t = 3):
        output.send(self.generate_output())

    def run(self, t = 3):
        self.pipe = Pipe()
        self.p = Process(target=self.run_inner, args = (self.pipe[0],t,))
        self.p.daemon = True
        self.p.start()

    def finish(self):
        if self.p is None:
            print("You need to run " + self.name + " before you finish!")
            return False
        self.p.join()
        #ignoring
        assert self.pipe[1].recv()
        return self.quit()

# just chills
class Idlebot(Bot):
    def __init__(self, server):
        super().__init__(server)
        self.name = "Idlebot"

    def run_inner(self, output, t = 3):
        time.sleep(t)
        output.send(self.generate_output())



# uses a random command {rate} times a second
class Randombot(Bot):
    def __init__(self, server, rate = 3):
        super().__init__(server)
        self.rate = rate
        self.name = 'Randombot'

    def generate_output(self):
        return (self.apiCallTimes)

    def run_inner(self, output, t = 3):
        calls = 0
        start = time.time()
        delay = 1.0 / self.rate
        while time.time()-start < t:
            calls += 1
            last = time.time()
            path, data = randomAPI()
            res = self.timedapi(path, data)
            assert res[0]
            #if not res[0]:
            #    print('oops',path,data,res[0].json())
            wait = delay - (time.time() - last)
            if wait > 0:
                time.sleep(wait)
        output.send(self.generate_output())

    def finish(self):
        if self.p is None:
            print("You need to run " + self.name + " before you finish!")
            return False
        self.p.join()
        self.apiCallTimes = self.pipe[1].recv()
        res = self.quit()
        return res


# disconnects and connects {rate} times a second
class Discobot(Bot):
    def __init__(self, server, rate = 3):
        super().__init__(server)
        self.rate = rate
        self.name = "Discobot"

    def generate_output(self,):
        return (self.token, self.apiCallTimes)

    def run_inner(self, output, t = 3):
        calls = 0
        start = time.time()
        delay = 1.0 / self.rate
        while time.time()-start < t:
            calls += 1
            last = time.time()
            assert self.timedapi('disconnect')
            resp = self.timedapi('connect')
            self.token = resp[0].json()['token']
            wait = delay - (time.time() - last)
            if wait > 0:
                time.sleep(wait)
        output.send(self.generate_output())

    def finish(self):
        if self.p is None:
            print("You need to run " + self.name + " before you finish!")
            return False
        self.p.join()
        self.token, self.apiCallTimes = self.pipe[1].recv()
        res = self.quit()
        return res