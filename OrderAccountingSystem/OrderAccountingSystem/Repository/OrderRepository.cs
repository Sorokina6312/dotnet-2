﻿using OrderAccountingSystem.Exceptions;
using OrderAccountingSystem.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OrderAccountingSystem.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private List<Order> _orders;
        private static SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly string _fileName = "Orders.xml";

        public OrderRepository()
        {
            _orders = new List<Order>();
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            await ReadOrdersFileAsync();
            return _orders;
        }

        public async Task<Guid> AddOrderAsync(Order order)
        {
            if (order.Customer == null || order.Products == null)
            {
                throw new ArgumentException();
            }
            order.Price = order.Products.Sum(f => f.Price);
            await ReadOrdersFileAsync();
            _orders.Add(order);
            await WriteOrdersFileAsync();
            return order.OrderId;
        }

        public async Task<Order> GetOrderAsync(Guid id)
        {
            await ReadOrdersFileAsync();
            foreach(Order order in _orders)
            {
                if (order.OrderId.Equals(id))
                {
                    return order;
                }
            }
            throw new NotFoundException();
        }

        public async Task<double> GetMonthlySales()
        {
            await ReadOrdersFileAsync();
            List<Order> orders = _orders.Where(f => f.Date.AddDays(30) > DateTime.Today).ToList();
            return orders.Sum(f => f.Price);
        }

        public async Task<Guid> DeleteOrderAsync(Guid id)
        {
            await ReadOrdersFileAsync();
            if (_orders.Remove(_orders.Find(f => f.OrderId == id)))
            {
                await WriteOrdersFileAsync();
                return id;
            }
            throw new NotFoundException();
        }

        public async Task<Guid> ChangeOrderAsync(Guid id, Order newOrder)
        {
            if (newOrder.Customer == null || newOrder.Products == null)
            {
                throw new ArgumentException();
            }
            await ReadOrdersFileAsync();
            foreach (Order order in _orders)
            {
                if (order.OrderId == id)
                {
                    order.Customer = newOrder.Customer;
                    order.Products = newOrder.Products;
                    order.Status = newOrder.Status;
                    order.Price = newOrder.Products.Sum(f => f.Price);
                    order.Date = newOrder.Date;
                    await WriteOrdersFileAsync();
                    return id;
                }
            }
            throw new NotFoundException();
        }

        public async Task<Guid> ChangeOrderStatusAsync(Guid id, Order newOrder)
        {
            await ReadOrdersFileAsync();
            foreach (Order order in _orders)
            {
                if (order.OrderId == id)
                {
                    order.Status = newOrder.Status;
                    await WriteOrdersFileAsync();
                    return id;
                }
            }
            throw new NotFoundException();
        }

        public async Task<bool> CheckOrderAsync(Guid id)
        {
            await ReadOrdersFileAsync();
            if (_orders.Find(f => f.OrderId.Equals(id)) != null)
            {
                return true;
            }
            return false;
        }

        public async Task ReadOrdersFileAsync()
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                if (!File.Exists(_fileName))
                {
                    _orders = new List<Order>();
                    return;
                }
                XmlSerializer formatter = new XmlSerializer(typeof(List<Order>));
                FileStream fs = new FileStream(_fileName, FileMode.OpenOrCreate);
                _orders = (List<Order>)formatter.Deserialize(fs);
                fs.Close();
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public async Task WriteOrdersFileAsync()
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                XmlSerializer formatter = new XmlSerializer(typeof(List<Order>));
                FileStream fs = new FileStream(_fileName, FileMode.Create);
                formatter.Serialize(fs, _orders);
                fs.Close();
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
    }
}
