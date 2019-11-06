from pytest import raises


def test_function():
    """A simple test case that always passes."""
    assert 1 > 0
    with raises(ZeroDivisionError):
        1/0
