# RubikCube
This program uses an evolutionary approach to solve the Full Rubik NxNxN Supercube, i.e. orientate all of the cubies, 
including the internal ones, not only according to face colors, but to the same orientation in 3D space. 
The problem is formally defined by the matrix representation using affine cubies transforms. 
The Full Supercube’s solving strategy uses a series of genetic algorithms that try to find a better cube configuration than the current one. 
Once found, movements are made to change the current configuration. 
This strategy is repeated until the cube is solved. 
The genetic algorithm limits the movements to the current cluster by solving the cube in stages, outwards from the center of the cube. 
The movements that solve the clusters are saved as macros and used to train and speed up the algorithm. 
The purpose of the presented algorithm is to minimize the solution time, and not necessarily the number of moves.
