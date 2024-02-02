/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-02-01 17:46:04
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-02-01 17:46:04
 * ScriptVersion: 0.1
 * ===============================================
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFExample
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class TableTest : MonoBehaviour
	{
        public class Student
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public int Level { get; set; }
        }
        public class TableChart : Table<Student>
        {

            protected Func<Student, string> NameGetter;

            public override void Add(Student item)
            {
                string _name = NameGetter(item);
            }

            public override void Clear()
            {
                throw new System.NotImplementedException();
            }

            public override IEnumerator<Student> Get()
            {
                throw new System.NotImplementedException();
            }

            public override IEnumerator<Student> GetEnumerator()
            {
                throw new System.NotImplementedException();
            }

            public override void OnDispose()
            {
                throw new System.NotImplementedException();
            }

            public override void Remove(Student item)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
