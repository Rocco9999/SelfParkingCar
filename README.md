<!DOCTYPE html>
<html lang="en">

<body>

<h1>Autonomous Parking System with Reinforcement Learning</h1>

<p>This project explores the implementation of an autonomous parking system using Reinforcement Learning (RL) algorithms within the Unity ML-Agents framework. The main objective is to train virtual agents capable of performing precision parking maneuvers and adapting to varying simulated conditions. Three independent models were developed and trained to reflect an increasing level of complexity, focusing on optimizing parking detection, penalty management, and minimizing the number of maneuvers required to achieve correct parking.</p>

<h2>Features</h2>
<ul>
    <li>Autonomous parking system implemented with RL in Unity ML-Agents.</li>
    <li>Three levels of complexity to test the agent's performance under different parking conditions.</li>
    <li>Optimization of parking detection, penalty management, and maneuver efficiency.</li>
</ul>

<h2>Requirements</h2>
<p>To run this project, you will need:</p>
<ul>
    <li>Unity 2019.4.28f1 or later</li>
    <li>Python 3.6 or later</li>
    <li>The following Python libraries:</li>
</ul>

<pre>
pip install mlagents==0.22.0 mlagents-envs==0.22.0 protobuf==3.19.6 grpcio==1.37.0 numpy==1.18.5 torch==1.8.1
</pre>

<h2>Project Structure</h2>
<ul>
    <li><strong>Training Scripts</strong>: Contains the Python scripts used for training the RL agent with Unity ML-Agents.</li>
    <li><strong>Unity Environment</strong>: The Unity project containing the environment setup, including parking scenarios of varying complexity.</li>
    <li><strong>Trained Models</strong>: Saved versions of the trained models for each complexity level.</li>
</ul>

<h2>Usage</h2>
<ol>
    <li>Clone the repository.</li>
    <li>Open the Unity project in Unity Editor.</li>
    <li>Ensure that the Python dependencies are installed.</li>
    <li>Run the training scripts to start training the agent or use the pre-trained models provided.</li>
</ol>

<h2>License</h2>
<p>This project is licensed under the MIT License.</p>

<h2>Author</h2>
<p>Developed by Rocco Fortunato / Daniele Dello Russo</p>

</body>
</html>
