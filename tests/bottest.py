import requests
import time
import pytest
import bots

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

def test_disco(server):
    n = 500
    t = 5
    discobots = [ bots.Discobot(server, 3) for i in range(n) ]
    for i in range(n):
        discobots[i].run(t)
    for i in range(n):
        assert discobots[i].finish() 

    MHT = max([ discobots[i].maxHangTime['connect'] for i in range(n) ])
    print('Highest wait time for connect = ' + str(MHT))