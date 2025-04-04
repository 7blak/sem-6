# Semester 6 - Computer Science Projects

This repository contains **team projects** I contributed to and **individual projects** completed during **Semester 6** of my **Computer Science** studies.

## 🤖 Agent Systems (agent-systems)  
**Technologies:** Java (JADE), Python (Mesa)  

This project includes code written in **Java using JADE** for **lab1, lab2, homework1**, and **Python using Mesa** for **lab3, lab4, homework2**.  
- **Homework1** simulates a **Grocery Delivery System**, where a client agent orders groceries and selects a delivery service from available options. Delivery agents interact with multiple markets to fulfill the order by choosing the best combination of prices and item availability.
- **JADE** is used to implement the system's agent-based logic, while **Mesa** is used for the Python-based lab projects.

---

## 🖼️ Computer Graphics (computer-graphics/lab1)  
**Technologies:** C# (WPF), XAML  

This project is a **Computer Graphics course** assignment, developed using **C# WPF** and **XAML**.  
It is an image filtering app that allows the user to apply various filters to an image, such as:  
- Functional filters: Invert colors, Brightness correction, Contrast enhancement, Gamma correction  
- Convolution filters: Gaussian blur, Emboss, Edge detection, Sharpen  
- Morphological filters: Erosion, Dilation  
- Dithering: Average dithering, K-means color quantization  

---

## 🚀 High Performance Computing (high-performance-computing)  
**Technologies:** C, MPI  

This folder contains **C code** written for the **High Performance Computing** course.  
It focuses on **MPI (Message Passing Interface)** to distribute computations across multiple machines and retrieve results.  

One example project is **matrix-vector multiplication**, which splits matrix calculations across different computers for parallel execution.  

### 🔹 Running the Code  
Clone the repository and navigate to the project folder:  
```sh
git clone https://github.com/7blak/sem-6 -b high-performance-computing
cd high-performance-computing/matrix-vector-mult
mpicc -o main main.c mmio.c -lm
```
Example execution:
```sh
mpirun --hostfile hosts.txt -np 4 ./mpi_matvec test.mtx # You need 4 hosts in a hosts.txt file
mpirun --mca btl_tcp_if_include eth0 -host p30413,p30414,p30415,p30410 -np 4 ./mpi_matvec bcsstk03.mtx # Example configuration using local PCs at our faculty
```

## 🔧 Software Engineering (software-engineering-2)  
**Technologies:** C#  

This folder contains all projects related to the **Software Engineering course**.  
- It includes an example **TDD Kata** written in **C#**.  
- A repository where we learned how to resolve pull requests during the labs.  
- During the rest of the subject, we will be developing an entire project called **DVS**, which will be linked here once completed.

---

📌 *Each project folder contains additional documentation and source code for reference.*
