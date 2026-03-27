using System;
using System.Collections.Generic;
using System.Linq;

namespace WarehouseKata;

public class WarehouseDeskApp
{
    private readonly Dictionary<string, int> stockBySku = new();
    private readonly Dictionary<string, int> reservedBySku = new();
    private readonly Dictionary<string, double> priceBySku = new();
    private readonly Dictionary<string, string> orderStatus = new();
    private readonly Dictionary<string, string> orderSku = new();
    private readonly Dictionary<string, int> orderQty = new();
    private readonly List<string> eventLog = new();
    private double cashBalance;
    private int nextOrderNumber;

    public void SeedData()
    {
        stockBySku["PEN-BLACK"] = 40;
        stockBySku["PEN-BLUE"] = 25;
        stockBySku["NOTE-A5"] = 15;
        stockBySku["STAPLER"] = 4;

        reservedBySku["PEN-BLACK"] = 0;
        reservedBySku["PEN-BLUE"] = 0;
        reservedBySku["NOTE-A5"] = 0;
        reservedBySku["STAPLER"] = 0;

        priceBySku["PEN-BLACK"] = 1.5;
        priceBySku["PEN-BLUE"] = 1.6;
        priceBySku["NOTE-A5"] = 4.0;
        priceBySku["STAPLER"] = 12.0;

        cashBalance = 300.0;
        nextOrderNumber = 1001;
    }

    public void RunDemoDay()
    {
        var commands = new List<string>
        {
            "RECV;NOTE-A5;5;2.20",
            "SELL;alice;PEN-BLACK;10",
            "SELL;bob;STAPLER;5",
            "CANCEL;O1002",
            "COUNT;STAPLER",
            "SELL;carol;STAPLER;2",
            "SELL;dan;NOTE-A5;14",
            "COUNT;NOTE-A5",
            "DUMP"
        };

        foreach (var command in commands)
            ProcessLine(command);

        PrintEndOfDayReport();
    }

    public void ProcessLine(string line)
    {
        var parts = line.Split(';');
        var type = parts[0];

        if (type == "RECV")
        {
            var sku = parts[1];
            var qty = int.Parse(parts[2].Trim());
            var unitCost = double.Parse(parts[3].Trim());
            stockBySku[sku] = stockBySku.GetValueOrDefault(sku, 0) + qty;
            cashBalance = cashBalance - (qty * unitCost);
            eventLog.Add($"received {qty} of {sku} at {unitCost}");
            return;
        }

        if (type == "SELL")
        {
            var customer = parts[1];
            var sku = parts[2];
            var qty = int.Parse(parts[3].Trim());
            var orderId = "O" + nextOrderNumber;
            nextOrderNumber = nextOrderNumber + 1;
            orderSku[orderId] = sku;
            orderQty[orderId] = qty;

            var onHand = stockBySku.GetValueOrDefault(sku, 0);
            var reserved = reservedBySku.GetValueOrDefault(sku, 0);
            var available = onHand - reserved;
            if (available < qty)
            {
                orderStatus[orderId] = "BACKORDER";
                eventLog.Add($"order {orderId} backordered for {customer} sku={sku} qty={qty}");
            }
            else
            {
                stockBySku[sku] = onHand - qty;
                var unitPrice = priceBySku.GetValueOrDefault(sku, 0.0);
                var orderTotal = unitPrice * qty;
                cashBalance = cashBalance + orderTotal;
                orderStatus[orderId] = "SHIPPED";
                eventLog.Add($"order {orderId} shipped to {customer} amount={orderTotal}");
            }
            return;
        }

        if (type == "CANCEL")
        {
            var orderId = parts[1];
            var status = orderStatus.GetValueOrDefault(orderId);
            if (status == null)
            {
                eventLog.Add($"cannot cancel {orderId} because it does not exist");
                return;
            }

            if (status == "BACKORDER")
            {
                orderStatus[orderId] = "CANCELLED";
                eventLog.Add($"cancelled backorder {orderId}");
                return;
            }

            if (status == "SHIPPED")
            {
                var sku = orderSku[orderId];
                var qty = orderQty.GetValueOrDefault(orderId, 0);
                var current = stockBySku.GetValueOrDefault(sku, 0);
                stockBySku[sku] = current + qty;
                var unitPrice = priceBySku.GetValueOrDefault(sku, 0.0);
                cashBalance = cashBalance - (unitPrice * qty);
                orderStatus[orderId] = "CANCELLED_AFTER_SHIP";
                eventLog.Add($"cancelled shipped order {orderId} with restock");
                return;
            }

            eventLog.Add($"order {orderId} could not be cancelled from state {status}");
            return;
        }

        if (type == "COUNT")
        {
            var sku = parts[1];
            var onHand = stockBySku.GetValueOrDefault(sku, 0);
            var reserved = reservedBySku.GetValueOrDefault(sku, 0);
            var available = onHand - reserved;
            eventLog.Add($"count {sku} onHand={onHand} reserved={reserved} available={available}");
            return;
        }

        if (type == "DUMP")
        {
            Console.WriteLine("---- dump ----");
            Console.WriteLine($"stock={FormatDict(stockBySku)}");
            Console.WriteLine($"reserved={FormatDict(reservedBySku)}");
            Console.WriteLine($"orders={FormatDict(orderStatus)}");
            Console.WriteLine($"cashBalance={cashBalance}");
            return;
        }

        eventLog.Add($"unknown command: {line}");
    }

    public void PrintEndOfDayReport()
    {
        int shipped = 0, backorder = 0, cancelled = 0;
        foreach (var status in orderStatus.Values)
        {
            if (status == "SHIPPED") shipped = shipped + 1;
            else if (status == "BACKORDER") backorder = backorder + 1;
            else if (status.StartsWith("CANCELLED")) cancelled = cancelled + 1;
        }

        var lowStock = new List<string>();
        foreach (var item in stockBySku)
        {
            if (item.Value < 5)
                lowStock.Add(item.Key);
        }

        Console.WriteLine();
        Console.WriteLine("==== end of day ====");
        Console.WriteLine($"orders shipped: {shipped}");
        Console.WriteLine($"orders backordered: {backorder}");
        Console.WriteLine($"orders cancelled: {cancelled}");
        Console.WriteLine($"cash balance: {cashBalance:F2}");
        Console.WriteLine($"low stock skus: [{string.Join(", ", lowStock)}]");
        Console.WriteLine();
        Console.WriteLine("events:");
        foreach (var evt in eventLog)
            Console.WriteLine($" - {evt}");
    }

    private static string FormatDict<TK, TV>(Dictionary<TK, TV> dict) where TK : notnull =>
        "{" + string.Join(", ", dict.Select(kvp => $"{kvp.Key}={kvp.Value}")) + "}";
}
