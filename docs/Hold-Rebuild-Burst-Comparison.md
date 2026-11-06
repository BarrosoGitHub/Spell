# Hold rebuild jobs

Hold rebuilds now always use the Job System/Burst path. The Managed/BurstJobs/Compare runtime selector has been removed. The independent managed detector remains available to the editor regression checks under **Tools > Destruction > Validate Shell Edges**.

Jobs build the spatial tree and match edge pairs. Final coordinates are reconstructed from the original source triangles using managed arithmetic to preserve exact hold spacing and ownership. Shell generation, cell ownership checks and GameObject/pool operations remain on the main thread. The job system uses its normal managed fallback if Burst is disabled globally.

Native input, segment, tree and index buffers retain their largest capacity for reuse and are released when the spawner is disabled. Returning a hold to the shared pool retains its object memory. Use Unity Profiler captures to inspect performance and memory usage.
