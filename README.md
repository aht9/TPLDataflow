# 🚀 High-Performance CSV Processing with TPL Dataflow in .NET

This project demonstrates how to process **large CSV files** efficiently using the **TPL Dataflow** library in .NET.  
By leveraging **BufferBlock**, **TransformBlock**, and **ActionBlock**, we can build a powerful and scalable **ETL-style pipeline** for concurrent and parallel data processing.

---

## 📌 Features
- **BufferBlock**: Acts as an input queue to handle backpressure.
- **TransformBlock**: Validates and maps raw CSV lines into strongly-typed objects.
- **ActionBlock**: Executes the final step (e.g., saving records to a database).
- **PropagateCompletion**: Ensures smooth flow of data and graceful shutdown of the pipeline.
- **BoundedCapacity & MaxDegreeOfParallelism**: Prevent overload and optimize concurrency.

---

## 📂 Example Scenario
Imagine a large CSV file containing millions of customer records.  
We want to:
1. Read the file line by line.
2. Validate and transform each line into a `CustomerRecord`.
3. Save records to the database concurrently without overloading resources.

---

## 🖥️ Sample Code

```csharp
var bufferBlock = new BufferBlock<string>(
    new DataflowBlockOptions { BoundedCapacity = 500 }
);

var transformBlock = new TransformBlock<string, CustomerRecord>(
    line =>
    {
        var columns = line.Split(',');
        if (columns.Length < 3) return null;

        return new CustomerRecord
        {
            Id = int.Parse(columns[0]),
            Name = columns[1],
            Email = columns[2]
        };
    },
    new ExecutionDataflowBlockOptions
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    });

var actionBlock = new ActionBlock<CustomerRecord>(
    async record =>
    {
        if (record == null) return;
        await SaveToDatabaseAsync(record);
        Console.WriteLine($"Processed: {record.Id}");
    },
    new ExecutionDataflowBlockOptions
    {
        MaxDegreeOfParallelism = 4,
        BoundedCapacity = 100
    });

// Link blocks together
bufferBlock.LinkTo(transformBlock, new DataflowLinkOptions { PropagateCompletion = true });
transformBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });

// Feed CSV lines into the pipeline
foreach (var line in File.ReadLines("bigdata.csv").Skip(1))
{
    await bufferBlock.SendAsync(line);
}

bufferBlock.Complete();
await actionBlock.Completion;
