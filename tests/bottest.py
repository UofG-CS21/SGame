import requests
import time
import pytest
import bots

def getMaxHangTime(bots):
    return max( [ max([ max(times) for times in bot.apiCallTimes.values()]) for bot in bots ] )

def test_init(server):
    bot1 = bots.Bot(server)
    bot1.run(3)
    bot2 = bots.Bot(server)
    bot2.run(3)

    assert bot1.finish()
    assert bot2.finish()


def test_idle(server):
    bot1 = bots.Idlebot(server)
    bot2 = bots.Idlebot(server)

    bot1.run(3)
    bot2.run(3)
    assert bot1.finish()
    assert bot2.finish()

def test_random(server):
    n = 10
    t = 5
    randombots = [ bots.Randombot(server, 50) for i in range(n) ]
    for i in range(n):
        randombots[i].run(t)
    for i in range(n):
        assert randombots[i].finish() 

    MHT = getMaxHangTime(randombots)
    print('Highest wait time for random = ' + str(MHT))

def test_disco(server):
    n = 10
    t = 5
    discobots = [ bots.Discobot(server, 50) for i in range(n) ]
    for i in range(n):
        discobots[i].run(t)
    for i in range(n):
        assert discobots[i].finish() 

    MHT = max([ max(discobots[i].apiCallTimes['connect']) for i in range(n) ])
    print('Highest wait time for connect = ' + str(MHT))