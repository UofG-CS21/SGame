import pytest


def test_function(host, port):
    """A simple test case that always passes."""
    assert 1 > 0
    assert port == 5000
    assert host == "localhost"
    with pytest.raises(ZeroDivisionError):
        1/0
