/*
 * Copyright 2014 Mikhail Shiryaev
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : ScadaData
 * Summary  : Event table for fast read data access
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2007
 * Modified : 2012
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Scada.Data
{
    /// <summary>
    /// Event table for fast read data access
    /// <para>������� ������� ��� �������� ������� � ������ �� ������</para>
    /// </summary>
    public class EventTableLight
    {
        /// <summary>
        /// ������ �������
        /// </summary>
        public class Event
        {
            /// <summary>
            /// �����������
            /// </summary>
            public Event()
            {
                Number = 0;
                DateTime = DateTime.MinValue;
                ObjNum = 0;
                KPNum = 0;
                ParamID = 0;
                CnlNum = 0;
                OldCnlVal = 0.0;
                OldCnlStat = 0;
                NewCnlVal = 0.0;
                NewCnlStat = 0;
                Checked = false;
                UserID = 0;
                Descr = "";
                Data = "";
            }

            /// <summary>
            /// �������� ��� ���������� ���������� ����� ������� � �����
            /// </summary>
            public int Number { get; set; }
            /// <summary>
            /// �������� ��� ���������� ��������� ����� �������
            /// </summary>
            public DateTime DateTime { get; set; }
            /// <summary>
            /// �������� ��� ���������� ����� �������
            /// </summary>
            public int ObjNum { get; set; }
            /// <summary>
            /// �������� ��� ���������� ����� ��
            /// </summary>
            public int KPNum { get; set; }
            /// <summary>
            /// �������� ��� ���������� ������������� ���������
            /// </summary>
            public int ParamID { get; set; }
            /// <summary>
            /// �������� ��� ���������� ����� �������� ������
            /// </summary>
            public int CnlNum { get; set; }
            /// <summary>
            /// �������� ��� ���������� ���������� �������� ������
            /// </summary>
            public double OldCnlVal { get; set; }
            /// <summary>
            /// �������� ��� ���������� ���������� ������ ������
            /// </summary>
            public int OldCnlStat { get; set; }
            /// <summary>
            /// �������� ��� ���������� ����� �������� ������
            /// </summary>
            public double NewCnlVal { get; set; }
            /// <summary>
            /// �������� ��� ���������� ����� ������ ������
            /// </summary>
            public int NewCnlStat { get; set; }
            /// <summary>
            /// �������� ��� ���������� �������, ��� ������� �����������
            /// </summary>
            public bool Checked { get; set; }
            /// <summary>
            /// �������� ��� ���������� ������������� �������������� ������� ������������
            /// </summary>
            public int UserID { get; set; }
            /// <summary>
            /// �������� ��� ���������� �������� �������
            /// </summary>
            public string Descr { get; set; }
            /// <summary>
            /// �������� ��� ���������� �������������� ������ �������
            /// </summary>
            public string Data { get; set; }
        }

        /// <summary>
        /// ������� ������� �������
        /// </summary>
        [FlagsAttribute]
        public enum EventFilters
        {
            /// <summary>
            /// ������ ������
            /// </summary>
            None = 0,
            /// <summary>
            /// ������ �� �������
            /// </summary>
            Obj = 1,
            /// <summary>
            /// ������ �� ��
            /// </summary>
            KP = 2,
            /// <summary>
            /// ������ �� ���������
            /// </summary>
            Param = 4,
            /// <summary>
            /// ������ �� �������
            /// </summary>
            Cnls = 8
        }

        /// <summary>
        /// ��� �������
        /// </summary>
        protected string tableName;
        /// <summary>
        /// ����� ���������� ��������� ����� �������
        /// </summary>
        protected DateTime fileModTime;
        /// <summary>
        /// ����� ��������� ��������� ���������� �������
        /// </summary>
        protected DateTime lastFillTime;

        /// <summary>
        /// ������ ���� �������
        /// </summary>
        protected List<Event> allEvents;
        /// <summary>
        /// ��������������� ������ �������
        /// </summary>
        protected List<Event> filteredEvents;
        /// <summary>
        /// ���������� ��� ���������� ������� ����� ���������������� ������ �������, 
        /// ������� � ������������ ������ �������
        /// </summary>
        protected List<Event> eventsCache;
        /// <summary>
        /// ���������� ��� ���������� ������� �������� ����� ���������������� ������ �������
        /// </summary>
        protected List<Event> lastEventsCache;
        /// <summary>
        /// ��������� ����� �������, �������� ��� ��������� eventsCache
        /// </summary>
        protected int startEvNum;
        /// <summary>
        /// ���������� ����������� ������� ��� ��������� lastEventsCache
        /// </summary>
        protected int lastEvCnt;

        /// <summary>
        /// ������� ������� �������
        /// </summary>
        protected EventFilters filters;
        /// <summary>
        /// ����� �������, �� �������� ����������� �������
        /// </summary>
        protected int objNumFilter;
        /// <summary>
        /// ����� ��, �� �������� ����������� �������
        /// </summary>
        protected int kpNumFilter;
        /// <summary>
        /// ����� ���������, �� �������� ����������� �������
        /// </summary>
        protected int paramNumFilter;
        /// <summary>
        /// ������������� ������ �������, �� ������� ����������� �������
        /// </summary>
        protected List<int> cnlsFilter;


        /// <summary>
        /// �����������
        /// </summary>
        public EventTableLight()
        {
            tableName = "";
            fileModTime = DateTime.MinValue;
            lastFillTime = DateTime.MinValue;

            allEvents = new List<Event>();
            filteredEvents = null;
            eventsCache = null;
            lastEventsCache = null;
            startEvNum = 0;
            lastEvCnt = 0;

            filters = EventFilters.None;
            objNumFilter = 0;
            kpNumFilter = 0;
            paramNumFilter = 0;
            cnlsFilter = null;
        }


        /// <summary>
        /// �������� ��� ���������� ��� ����� �������
        /// </summary>
        public string TableName
        {
            get
            {
                return tableName;
            }
            set
            {
                if (tableName != value)
                {
                    tableName = value;
                    fileModTime = DateTime.MinValue;
                    lastFillTime = DateTime.MinValue;
                }
            }
        }

        /// <summary>
        /// �������� ��� ���������� ����� ���������� ��������� ����� �������
        /// </summary>
        public DateTime FileModTime
        {
            get
            {
                return fileModTime;
            }
            set
            {
                fileModTime = value;
            }
        }

        /// <summary>
        /// �������� ��� ���������� ����� ��������� ��������� ���������� �������
        /// </summary>
        public DateTime LastFillTime
        {
            get
            {
                return lastFillTime;
            }
            set
            {
                fileModTime = value;
            }
        }
        

        /// <summary>
        /// �������� ������ ���� �������
        /// </summary>
        public List<Event> AllEvents
        {
            get
            {
                return allEvents;
            }
        }

        /// <summary>
        /// �������� ��������������� ������ �������
        /// </summary>
        public List<Event> FilteredEvents
        {
            get
            {
                if (filteredEvents == null)
                {
                    // �������� � ���������� ���������������� ������ �������
                    filteredEvents = new List<Event>();
                    foreach (Event ev in allEvents)
                    {
                        if (EventVisible(ev))
                            filteredEvents.Add(ev);
                    }
                }                
                return filteredEvents;
            }
        }

        /// <summary>
        /// �������� ��� ���������� ������� ������� �������
        /// </summary>
        public EventFilters Filters
        {
            get
            {
                return filters;
            }
            set
            {
                if (filters != value)
                {
                    filters = value;
                    filteredEvents = null;
                    eventsCache = null;
                    lastEventsCache = null;
                }
            }
        }

        /// <summary>
        /// �������� ��� ���������� ����� �������, �� �������� ����������� �������
        /// </summary>
        public int ObjNumFilter
        {
            get
            {
                return objNumFilter;
            }
            set
            {
                if (objNumFilter != value)
                {
                    objNumFilter = value;
                    filteredEvents = null;
                    eventsCache = null;
                    lastEventsCache = null;
                }
            }
        }

        /// <summary>
        /// �������� ��� ���������� ����� ��, �� �������� ����������� �������
        /// </summary>
        public int KPNumFilter
        {
            get
            {
                return kpNumFilter;
            }
            set
            {
                if (kpNumFilter != value)
                {
                    kpNumFilter = value;
                    filteredEvents = null;
                    eventsCache = null;
                    lastEventsCache = null;
                }
            }
        }

        /// <summary>
        /// �������� ��� ���������� ����� ���������, �� �������� ����������� �������
        /// </summary>
        public int ParamNumFilter
        {
            get
            {
                return paramNumFilter;
            }
            set
            {
                if (paramNumFilter != value)
                {
                    paramNumFilter = value;
                    filteredEvents = null;
                    eventsCache = null;
                    lastEventsCache = null;
                }
            }
        }

        /// <summary>
        /// �������� ��� ���������� ������ �������, �� �������� ����������� �������
        /// </summary>
        public List<int> CnlsFilter
        {
            get
            {
                return cnlsFilter;
            }
            set
            {
                cnlsFilter = value;
                if (cnlsFilter != null)
                    cnlsFilter.Sort();
                filteredEvents = null;
                eventsCache = null;
                lastEventsCache = null;
            }
        }


        /// <summary>
        /// ���������, ��� ������� �������� ������� � �������������� ���������
        /// </summary>
        protected bool EventVisible(Event ev)
        {
            return !((filters & EventFilters.Obj) > 0 && ev.ObjNum != objNumFilter ||
                (filters & EventFilters.KP) > 0 && ev.KPNum != kpNumFilter ||
                (filters & EventFilters.Param) > 0 && ev.ParamID != paramNumFilter ||
                (filters & EventFilters.Cnls) > 0 && (cnlsFilter == null || cnlsFilter.BinarySearch(ev.CnlNum) < 0));
        }

        /// <summary>
        /// �������� ������� � �������
        /// </summary>
        public void AddEvent(Event ev)
        {
            allEvents.Add(ev);
            filteredEvents = null;
            eventsCache = null;
            lastEventsCache = null;
        }

        /// <summary>
        /// �������� ������� �������
        /// </summary>
        public void Clear()
        {
            allEvents.Clear();
            filteredEvents = null;
            eventsCache = null;
            lastEventsCache = null;
        }

        /// <summary>
        /// �������� ����� ���������������� ������ �������, ������� � ��������� ������ �������
        /// </summary>
        public List<Event> GetEvents(int startEvNum)
        {
            if (eventsCache == null || this.startEvNum != startEvNum)
            {
                eventsCache = new List<Event>();
                this.startEvNum = startEvNum;

                int ind = startEvNum < 0 ? 0 : startEvNum - 1;
                int cnt = allEvents.Count;

                while (ind < cnt)
                {
                    Event ev = allEvents[ind];
                    if (EventVisible(ev))
                        eventsCache.Add(ev);
                    ind++;
                }
            }

            return eventsCache;
        }

        /// <summary>
        /// �������� �������� ����� ���������������� ������ �������
        /// </summary>
        public List<Event> GetLastEvents(int count)
        {
            if (lastEventsCache == null || lastEvCnt != count)
            {
                lastEventsCache = new List<Event>();
                lastEvCnt = count;

                if (count > 0)
                {
                    if (filteredEvents == null)
                    {
                        int ind = allEvents.Count - 1;
                        int cnt = 0; // ���������� ���������� �������

                        while (ind >= 0 && cnt < count)
                        {
                            Event ev = allEvents[ind];
                            if (EventVisible(ev))
                            {
                                lastEventsCache.Insert(0, ev);
                                cnt++;
                            }
                            ind--;
                        }
                    }
                    else
                    {
                        int cnt = filteredEvents.Count < count ? filteredEvents.Count : count;
                        if (cnt > 0)
                            lastEventsCache.AddRange(filteredEvents.GetRange(filteredEvents.Count - cnt, cnt));
                    }
                }
            }

            return lastEventsCache;
        }
    }
}
