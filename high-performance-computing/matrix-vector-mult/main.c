#include <stdio.h>
#include <stdlib.h>
#include <mpi.h>
#include "mmio.h"
#include <time.h>
#include <string.h>

// Structure to hold the CSR matrix
typedef struct {
    int nrows;
    int ncols;
    int nnz;
    int *row_ptr;  // Array of size nrows+1
    int *col_idx;  // Array of size nnz
    double *val;   // Array of size nnz
} CSRMatrix;

// Function to convert a matrix in COO format to CSR format
// coo_row, coo_col, coo_val are arrays of length nnz
// Assumes input is 1-indexed (as in Matrix Market) and converts to 0-indexed
void coo_to_csr(int nrows, int nnz, int *coo_row, int *coo_col, double *coo_val, CSRMatrix *mat) {
    int i;
    mat->nrows = nrows;
    // Assume square matrix so ncols = nrows
    mat->ncols = nrows;
    mat->nnz = nnz;
    mat->row_ptr = (int *) calloc(nrows+1, sizeof(int));
    mat->col_idx = (int *) malloc(nnz * sizeof(int));
    mat->val     = (double *) malloc(nnz * sizeof(double));
    
    // Count number of entries per row
    for (i = 0; i < nnz; i++) {
        // Convert from 1-indexed to 0-indexed
        int row = coo_row[i] - 1;
        mat->row_ptr[row+1]++;
    }
    // Cumulative sum to get row_ptr
    for (i = 0; i < nrows; i++) {
        mat->row_ptr[i+1] += mat->row_ptr[i];
    }
    // Temporary copy of row_ptr to use as offset counter
    int *temp = (int *) malloc(nrows * sizeof(int));
    for (i = 0; i < nrows; i++)
        temp[i] = mat->row_ptr[i];
    
    // Fill col_idx and val arrays
    for (i = 0; i < nnz; i++) {
        int row = coo_row[i] - 1;
        int dest = temp[row];
        mat->col_idx[dest] = coo_col[i] - 1;  // convert to 0-index
        mat->val[dest] = coo_val[i];
        temp[row]++;
    }
    free(temp);
}

int main(int argc, char *argv[]) {
    int rank, size;
    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &rank);
    MPI_Comm_size(MPI_COMM_WORLD, &size);
    
    CSRMatrix A;
    double *x = NULL; // input vector
    double *y = NULL; // result vector
    int n;           // matrix dimension
    int isTest = 0;  // flag to indicate if we are using "text.mtx"
    if (strcmp(argv[1], "test.mtx") == 0)
        isTest = 1;
    
    // Variables used when reading matrix in process 0
    int ret_code;
    MM_typecode matcode;
    FILE *f;
    int M, N, nz;
    
    // Only process 0 reads the matrix from file
    if (rank == 0) {
        if (argc < 2) {
            fprintf(stderr, "Usage: %s [matrix-market-filename]\n", argv[0]);
            MPI_Abort(MPI_COMM_WORLD, 1);
        }
        if ((f = fopen(argv[1], "r")) == NULL) {
            fprintf(stderr, "Could not open file %s\n", argv[1]);
            MPI_Abort(MPI_COMM_WORLD, 1);
        }
        if (mm_read_banner(f, &matcode) != 0) {
            fprintf(stderr, "Could not process Matrix Market banner.\n");
            MPI_Abort(MPI_COMM_WORLD, 1);
        }
        // We require a sparse matrix in coordinate format
        if (!mm_is_matrix(matcode) || !mm_is_sparse(matcode)) {
            fprintf(stderr, "Only sparse matrix in Matrix Market format supported.\n");
            MPI_Abort(MPI_COMM_WORLD, 1);
        }
        // Read matrix size and number of nonzeros
        if (mm_read_mtx_crd_size(f, &M, &N, &nz) != 0) {
            fprintf(stderr, "Could not read matrix size.\n");
            MPI_Abort(MPI_COMM_WORLD, 1);
        }
        // Check minimum size condition
        if ((M < 100 || N < 100) && !isTest) {
            fprintf(stderr, "Matrix dimensions must be at least 100.\n");
            MPI_Abort(MPI_COMM_WORLD, 1);
        }
        // Allocate arrays for COO format
        int *coo_row = (int *) malloc(nz * sizeof(int));
        int *coo_col = (int *) malloc(nz * sizeof(int));
        double *coo_val = (double *) malloc(nz * sizeof(double));
        
        for (int i = 0; i < nz; i++) {
            // Read one entry. Adjust reading based on real/double data.
            ret_code = fscanf(f, "%d %d %lf", &coo_row[i], &coo_col[i], &coo_val[i]);
            if (ret_code != 3) {
                fprintf(stderr, "Error reading entry %d from file\n", i);
                MPI_Abort(MPI_COMM_WORLD, 1);
            }
        }
        fclose(f);
        
        // Convert COO to CSR format
        coo_to_csr(M, nz, coo_row, coo_col, coo_val, &A);
        n = M;
        free(coo_row);
        free(coo_col);
        free(coo_val);
        
        // Generate a random vector x of length n
        x = (double *) malloc(n * sizeof(double));
        if (isTest && n == 10) {
            x = (double *) malloc(n * sizeof(double));
            double fixed_x[10] = {0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0};
            memcpy(x, fixed_x, 10 * sizeof(double));
        } else {
            // Otherwise, generate a random vector
            x = (double *) malloc(n * sizeof(double));
            srand(time(NULL));
            for (int i = 0; i < n; i++) {
                x[i] = (double)rand() / RAND_MAX;
            }
        }
        double fixed_x[10] = {0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0};
        memcpy(x, fixed_x, 10 * sizeof(double));
        // Allocate result vector y
        y = (double *) calloc(n, sizeof(double));
    }
    
    // Broadcast the dimension n to all processes
    MPI_Bcast(&n, 1, MPI_INT, 0, MPI_COMM_WORLD);
    
    // Broadcast the vector x to all processes
    if (rank != 0) {
        x = (double *) malloc(n * sizeof(double));
    }
    MPI_Bcast(x, n, MPI_DOUBLE, 0, MPI_COMM_WORLD);
    
    // Partition the matrix rows among processes.
    // Determine local rows for each process.
    int local_start, local_end, local_nrows;
    int base = n / size;
    int rem = n % size;
    if (rank < rem) {
        local_nrows = base + 1;
        local_start = rank * local_nrows;
    } else {
        local_nrows = base;
        local_start = rank * local_nrows + rem;
    }
    local_end = local_start + local_nrows;
    
    // Each process will need its own local CSR submatrix.
    int local_nnz;
    int *local_row_ptr = NULL;
    int *local_col_idx = NULL;
    double *local_val = NULL;
    
    if (rank == 0) {
        // Process 0 sends the appropriate parts to each process.
        // For itself, allocate local arrays.
        local_nnz = A.row_ptr[local_end] - A.row_ptr[local_start];
        local_row_ptr = (int *) malloc((local_nrows + 1) * sizeof(int));
        local_col_idx = (int *) malloc(local_nnz * sizeof(int));
        local_val     = (double *) malloc(local_nnz * sizeof(double));
        
        // Copy local CSR portion (adjust row_ptr indices to start at 0)
        for (int i = 0; i < local_nrows + 1; i++) {
            local_row_ptr[i] = A.row_ptr[local_start + i] - A.row_ptr[local_start];
        }
        for (int i = A.row_ptr[local_start]; i < A.row_ptr[local_end]; i++) {
            int pos = i - A.row_ptr[local_start];
            local_col_idx[pos] = A.col_idx[i];
            local_val[pos] = A.val[i];
        }
        
        // Now send to each other process their portion
        for (int p = 1; p < size; p++) {
            int p_local_start, p_local_end, p_local_nrows;
            if (p < rem) {
                p_local_nrows = base + 1;
                p_local_start = p * p_local_nrows;
            } else {
                p_local_nrows = base;
                p_local_start = p * p_local_nrows + rem;
            }
            p_local_end = p_local_start + p_local_nrows;
            int p_local_nnz = A.row_ptr[p_local_end] - A.row_ptr[p_local_start];
            
            // Send number of local rows and nnz
            MPI_Send(&p_local_nrows, 1, MPI_INT, p, 0, MPI_COMM_WORLD);
            MPI_Send(&p_local_nnz, 1, MPI_INT, p, 0, MPI_COMM_WORLD);
            
            // Prepare temporary row_ptr adjusted to local indexing
            int *temp_row_ptr = (int *) malloc((p_local_nrows + 1) * sizeof(int));
            for (int i = 0; i < p_local_nrows + 1; i++) {
                temp_row_ptr[i] = A.row_ptr[p_local_start + i] - A.row_ptr[p_local_start];
            }
            MPI_Send(temp_row_ptr, p_local_nrows + 1, MPI_INT, p, 0, MPI_COMM_WORLD);
            free(temp_row_ptr);
            
            // Send col_idx and val arrays
            MPI_Send(A.col_idx + A.row_ptr[p_local_start], p_local_nnz, MPI_INT, p, 0, MPI_COMM_WORLD);
            MPI_Send(A.val + A.row_ptr[p_local_start], p_local_nnz, MPI_DOUBLE, p, 0, MPI_COMM_WORLD);
        }
    } else {
        // Worker processes receive their local CSR data
        MPI_Recv(&local_nrows, 1, MPI_INT, 0, 0, MPI_COMM_WORLD, MPI_STATUS_IGNORE);
        MPI_Recv(&local_nnz, 1, MPI_INT, 0, 0, MPI_COMM_WORLD, MPI_STATUS_IGNORE);
        local_row_ptr = (int *) malloc((local_nrows + 1) * sizeof(int));
        MPI_Recv(local_row_ptr, local_nrows + 1, MPI_INT, 0, 0, MPI_COMM_WORLD, MPI_STATUS_IGNORE);
        local_col_idx = (int *) malloc(local_nnz * sizeof(int));
        MPI_Recv(local_col_idx, local_nnz, MPI_INT, 0, 0, MPI_COMM_WORLD, MPI_STATUS_IGNORE);
        local_val = (double *) malloc(local_nnz * sizeof(double));
        MPI_Recv(local_val, local_nnz, MPI_DOUBLE, 0, 0, MPI_COMM_WORLD, MPI_STATUS_IGNORE);
    }
    
    // Each process computes its local portion of y (matrix-vector multiplication)
    double *local_y = (double *) calloc(local_nrows, sizeof(double));
    for (int i = 0; i < local_nrows; i++) {
        for (int j = local_row_ptr[i]; j < local_row_ptr[i+1]; j++) {
            int col = local_col_idx[j];
            local_y[i] += local_val[j] * x[col];
        }
    }
    
    // Gather results to process 0
    int *recvcounts = NULL;
    int *displs = NULL;
    if (rank == 0) {
        y = (double *) calloc(n, sizeof(double));
        recvcounts = (int *) malloc(size * sizeof(int));
        displs = (int *) malloc(size * sizeof(int));
    }

    if (isTest) {
        // Use an ordered printing loop so each process prints in turn
        for (int p = 0; p < size; p++) {
            if (rank == p) {
                printf("Process %d (global row range: %d to %d) received matrix entries:\n", rank, local_start, local_end - 1);
                for (int i = 0; i < local_nrows; i++) {
                    int global_row = local_start + i;
                    for (int j = local_row_ptr[i]; j < local_row_ptr[i+1]; j++) {
                        printf("  (%d, %d) -> %lf\n", global_row, local_col_idx[j], local_val[j]);
                    }
                }
                fflush(stdout);
            }
            MPI_Barrier(MPI_COMM_WORLD);
        }
    }
    
    // Each process sends its number of local rows to process 0
    int local_count = local_nrows;
    MPI_Gather(&local_count, 1, MPI_INT, recvcounts, 1, MPI_INT, 0, MPI_COMM_WORLD);
    
    if (rank == 0) {
        displs[0] = 0;
        for (int i = 1; i < size; i++) {
            displs[i] = displs[i-1] + recvcounts[i-1];
        }
    }
    
    MPI_Gatherv(local_y, local_nrows, MPI_DOUBLE, y, recvcounts, displs, MPI_DOUBLE, 0, MPI_COMM_WORLD);
    
    if (rank == 0) {
        // Print a snippet of the result vector (for example, first 10 entries)
        printf("Result vector y (first 10 entries):\n");
        for (int i = 0; i < (n < 10 ? n : 10); i++) {
            printf("%lf\n", y[i]);
        }
    }
    
    // Free allocated memory
    free(x);
    free(local_y);
    free(local_row_ptr);
    free(local_col_idx);
    free(local_val);
    if (rank == 0) {
        free(A.row_ptr);
        free(A.col_idx);
        free(A.val);
        free(y);
        free(recvcounts);
        free(displs);
    }
    
    MPI_Finalize();
    return 0;
}
