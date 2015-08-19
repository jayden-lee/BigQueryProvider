﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace DevExpress.DataAccess.BigQuery {
    public class BigQueryParameterCollection : DbParameterCollection {
        static void ValidateType(object value) {
            if(!(value is BigQueryParameter))
                throw new ArgumentException("Invalid parameter type");
        }

        object syncRoot;
        readonly List<BigQueryParameter> innerList = new List<BigQueryParameter>();

        public override int Count {
            get { return innerList.Count; }
        }

        public override bool IsSynchronized { get { return false; } }

        public override object SyncRoot {
            get {
                if(syncRoot == null) {
                    Interlocked.CompareExchange(ref syncRoot, new object(), null);
                }
                return syncRoot;
            }
        }

        public override bool IsFixedSize {
            get { return false; }
        }

        public override bool IsReadOnly {
            get { return false; }
        }

        public int Add(string parameterName, DbType dbType) {
            return Add(new BigQueryParameter(parameterName, dbType));
        }

        public override int IndexOf(string parameterName) {
            BigQueryParameter value = innerList.FirstOrDefault(p => p.ParameterName == parameterName);
            return IndexOf(value);
        }

        public override int IndexOf(object value) {
            if(value == null)
                return -1;
            ValidateType(value);

            for(int i = 0; i < innerList.Count; i++) {
                if(innerList[i] == value)
                    return i;
            }
            return -1;
        }

        public override IEnumerator GetEnumerator() {
            return innerList.GetEnumerator();
        }

        public override void Remove(object value) {
            ValidateType(value);
            int index = IndexOf(value);
            if(index >= 0)
                RemoveAt(index);
            else {
                throw new InvalidOperationException("Item to remove not found");
            }
        }

        public override void RemoveAt(int index) {
            RangeCheck(index);
            RemoveIndex(index);
        }

        public override void RemoveAt(string parameterName) {
            RemoveIndex(CheckName(parameterName));
        }

        public override int Add(object parameter) {
            if(parameter == null)
                throw new ArgumentNullException("parameter");
            ValidateType(parameter);
            innerList.Add((BigQueryParameter)parameter);
            return Count - 1;
        }

        public override bool Contains(object value) {
            return IndexOf(value) >= 0;
        }

        public override void Clear() {
            innerList.Clear();
        }

        public override void Insert(int index, object value) {
            ValidateType(value);
            innerList.Insert(index, (BigQueryParameter)value);
        }

        public override bool Contains(string parameterName) {
            return IndexOf(parameterName) != -1;
        }

        public override void CopyTo(Array array, int index) {
            ((ICollection)innerList).CopyTo(array, index);
        }

        public override void AddRange(Array values) {
            innerList.AddRange(values.OfType<BigQueryParameter>().ToArray());
        }

        internal void Validate() {
            CheckDuplicateNames();
            foreach(var parameter in innerList) {
                parameter.Validate();
            }
        }

        protected override DbParameter GetParameter(int index) {
            RangeCheck(index);
            return innerList[index];
        }

        protected override DbParameter GetParameter(string parameterName) {
            int index = IndexOf(parameterName);
            if(index < 0)
                throw new IndexOutOfRangeException();
            return innerList[index];
        }

        protected override void SetParameter(int index, DbParameter value) {
            ValidateType(value);
            RangeCheck(index);
            Replace(index, value);
        }

        protected override void SetParameter(string parameterName, DbParameter value) {
            int index = IndexOf(parameterName);
            if(index < 0)
                throw new ArgumentException("Wrong parameter name");
            Replace(index, value);
        }

        void RemoveIndex(int index) {
            innerList.RemoveAt(index);
        }

        void RangeCheck(int index) {
            if(index < 0 || Count <= index)
                throw new IndexOutOfRangeException();
        }

        int CheckName(string parameterName) {
            int index = IndexOf(parameterName);
            if(index < 0)
                throw new ArgumentException("Wrong parameter name");
            return index;
        }

        void ValidateParameter(int index, DbParameter value) {
            if(value == null)
                throw new NullReferenceException("parameter");
            if(index == IndexOf(value))
                return;
            if(!string.IsNullOrEmpty(value.ParameterName))
                return;
            string parameterName;
            index = 1;
            do {
                parameterName = "Parameters" + index.ToString(CultureInfo.CurrentCulture);
                index++;
            }
            while(IndexOf(parameterName) != -1);

            value.ParameterName = parameterName;
        }

        void Replace(int index, DbParameter value) {
            ValidateType(value);
            ValidateParameter(index, value);
            innerList[index] = (BigQueryParameter)value;
        }

        void CheckDuplicateNames() {
            HashSet<string> set = new HashSet<string>();
            foreach(var bigQueryParameter in innerList) {
                if(set.Contains(bigQueryParameter.ParameterName)) {
                    throw new DuplicateNameException("Parameter collection contains duplicate parameters with name '" + bigQueryParameter.ParameterName + "'");
                }
                set.Add(bigQueryParameter.ParameterName);
            }
        }
    }
}
