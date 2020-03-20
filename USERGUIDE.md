# Team CS21 Main Project - Team Project 3 Course 2019-20

# User Guide

## API Documentation

<table class="tg">
  <tr>
    <th class="tg-cly1">Name</th>
    <th class="tg-cly1">Parameters</th>
    <th class="tg-cly1">Return Values</th>
    <th class="tg-cly1">Description</th>
  </tr>
  <tr>
    <td class="tg-cly1">connect</td>
    <td class="tg-cly1">None</td>
    <td class="tg-cly1">"token" : new ship token for user (string)</td>
    <td class="tg-cly1">Establishes connection from client to server</td>
  </tr>
  <tr>
    <td class="tg-cly1">disconnect</td>
    <td class="tg-cly1">"token": ship's token (string)</td>
    <td class="tg-cly1">None</td>
    <td class="tg-cly1">Disconnects player whose token is 'token'</td>
  </tr>
  <tr>
    <td class="tg-cly1">accelerate</td>
    <td class="tg-cly1">"token": ship's token (string), " x" : magnitude of acceleration in x-direction (double), "y" : magnitude of acceleration in y-direction (double)</td>
    <td class="tg-cly1">None</td>
    <td class="tg-cly1">Accelerates ship in given direction. Magnitude of accelerations is what the player chose, scaled down if the player does not have enough energy</td>
  </tr>
  <tr>
    <td class="tg-0lax">getShipInfo</td>
    <td class="tg-0lax">"token" : ship's token (string)</td>
    <td class="tg-0lax">"id" : ship's id (string), "area" : ship's area (double) , "energy" : ship's energy (double), "posX" : ship's x position (double), "posY" : ship's y position (double), "velX" : ship's velocity in x-axis (double), "velY" : ship's velocity in y-axis (double), "shieldWidth" : ship's shield width (double), "shieldDir" : ship's shield direction (double)</td>
    <td class="tg-0lax">Returns relevant information of the ship</td>
  </tr>
  <tr>
    <td class="tg-0lax">scan</td>
    <td class="tg-0lax">"token" : ship's token (string), "direction" : direction of scan (double), "width" : width of scan (double), "energy": energy spent on scan (int)</td>
    <td class="tg-0lax">Array of { "id" : struck ship's id (string), "area" : struck ship's area (double), "posX" : struck ship's x position (double), "posY" : struck ship's y position (double) }</td>
    <td class="tg-0lax">Scans a cone around "direction", of width 2 * "width" degrees. The range of the scan depends directly on how much energy is supplied.</td>
  </tr>
  <tr>
    <td class="tg-0lax">shoot</td>
    <td class="tg-0lax">"token" : ship's token (string), "direction" : direction of shot (double), "width" : width of shot (double), "energy": energy to expend on the shot (int), "damage" : the damage -used for scaling (double)</td>
    <td class="tg-0lax">Array of { "id" : struck ship's id (string), "area" : struck ship's area (double), "posX" : struck ship's x position (double), "posY" : struck ship's y position (double) }</td>
    <td class="tg-0lax">Fires an energy cone around "direction", of width 2 * "width" degrees. Power depends on the energy supplied.</td>
  </tr>
  <tr>
    <td class="tg-0lax">shield</td>
    <td class="tg-0lax">"token" : ship's token (string), "direction" : the centre angle of the shield (double), "width" : the half-width of the shield (double)</td>
    <td class="tg-0lax">None</td>
    <td class="tg-0lax">Sets the shield direction and radius around the ship</td>
  </tr>
</table>
