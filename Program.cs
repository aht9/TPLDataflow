/*
 * TPL Dataflow Example for .NET Core
 * 
 * This file demonstrates the implementation of a data processing pipeline using TPL Dataflow.
 * The example shows how to process CSV data through multiple stages with different levels of parallelism.
 *
 * Author: AHT9
 * GitHub: https://github.com/Aht9/TPLDataflow
 * Email: Amirht97@gmail.com
 *
 * Key Components:
 * - BufferBlock: Acts as a buffer for incoming data
 * - TransformBlock: Transforms CSV lines into CustomerRecord objects
 * - ActionBlock: Handles the processed records (e.g., saving to database)
 * 
 * Last Updated: September 22, 2025
 */

using System.Threading.Tasks.Dataflow;

namespace TPLDataflow;

class Program
{
    static async Task Main(string[] args)
    {
        string filePath = "bigdata.csv";

        // مرحله 1: BufferBlock برای صف داده‌ها
        var bufferBlock = new BufferBlock<string>(
            new DataflowBlockOptions { BoundedCapacity = 500 }
        );
        
        // مرحله 2: TransformBlock برای تبدیل خط CSV به رکورد معتبر
        var transformBlock = new TransformBlock<string, CustomerRecord>(
            line =>
            {
                var columns = line.Split(',');

                // اعتبارسنجی داده
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
        
        // مرحله 3: ActionBlock برای ذخیره رکورد در دیتابیس
        var actionBlock = new ActionBlock<CustomerRecord>(
            async record =>
            {
                if (record == null) return;
                await SaveToDatabaseAsync(record);
                Console.WriteLine($"Processed: {record.Id}");
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 4, // کنترل همزمانی در DB
                BoundedCapacity = 100
            });
        
        
        // اتصال بلاک‌ها (Pipeline)
        bufferBlock.LinkTo(transformBlock, new DataflowLinkOptions { PropagateCompletion = true });
        transformBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });

        
        // خواندن خط به خط فایل و ارسال به BufferBlock
        foreach (var line in File.ReadLines(filePath).Skip(1)) // Skip header
        {
            await bufferBlock.SendAsync(line);
        }

        // پایان پردازش
        bufferBlock.Complete();
        await actionBlock.Completion;

        
        Console.WriteLine("Processing completed.");
    }
    
    static Task SaveToDatabaseAsync(CustomerRecord record)
    {
        // شبیه‌سازی ذخیره در DB
        return Task.Delay(50);
    }
}

public class CustomerRecord
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}