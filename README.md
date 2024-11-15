# Autonomous Parking System with Reinforcement Learning

This project explores the implementation of an autonomous parking system using Reinforcement Learning (RL) algorithms within the Unity ML-Agents framework. The main objective is to train virtual agents capable of performing precision parking maneuvers and adapting to varying simulated conditions. Three independent models were developed and trained to reflect an increasing level of complexity, focusing on optimizing parking detection, penalty management, and minimizing the number of maneuvers required to achieve correct parking.

## Demo Video

[![Demo Video](https://img.youtube.com/vi/y1EpDRMuOqk/0.jpg)](https://www.youtube.com/watch?v=y1EpDRMuOqk)

Click the image above to watch the demo video on YouTube.

## Features

- Autonomous parking system implemented with RL in Unity ML-Agents.
- Three levels of complexity to test the agent's performance under different parking conditions.
- Optimization of parking detection, penalty management, and maneuver efficiency.

## Project Structure

- Training Scripts: Contains the Python scripts used for training the RL agent with Unity ML-Agents.
- Unity Environment: The Unity project containing the environment setup, including parking scenarios of varying complexity.
- Trained Models: Saved versions of the trained models for each complexity level.

## Requirements

To run this project, you will need:

- Unity 2019.4.28f1 or later
- Python 3.6 or later
- The following Python libraries:
  
pip install mlagents==0.22.0 mlagents-envs==0.22.0 protobuf==3.19.6 grpcio==1.37.0 numpy==1.18.5 torch==1.8.1

## Usage

- Clone the repository
- Open the Unity project in Unity Editor.
- Ensure that the Python dependencies are installed.
- Run the training scripts to start training the agent or use the pre-trained models provided.
        
## Author

Developed by:

- Rocco Fortunato
- Daniele Dello Russo
