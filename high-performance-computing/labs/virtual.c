#include <stdio.h>
#include <stdlib.h>
#include <mpi.h>

int main(int argc, char *argv[]) {
    int myRank, numProcs, nodes = 4, index[] = {2, 4, 5, 6}, edges[] = {1, 2, 0, 3, 0, 1};

    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &myRank);
    MPI_Comm_size(MPI_COMM_WORLD, &numProcs);

    MPI_Comm graph_comm;
    MPI_Graph_create(MPI_COMM_WORLD, nodes, index, edges, 0, &graph_comm);

    int new_rank;
    MPI_Comm_rank(graph_comm, &new_rank);

    int neighbours_count;
    MPI_Graph_neighbors_count(graph_comm, new_rank, &neighbours_count);

    int neighbours[neighbours_count];
    MPI_Graph_neighbors(graph_comm, new_rank, neighbours_count, neighbours);

    printf("Process %d has %d neighbours: ", new_rank, neighbours_count);
    for (int i = 0; i < neighbours_count; i++)
    {
        printf("%d ", neighbours[i]);
    }
    printf("\n");

    MPI_Comm_free(&graph_comm);
    MPI_Finalize();
    return 0;
}