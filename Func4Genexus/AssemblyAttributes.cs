// Atributos obrigatórios para GeneXus 18
// PackageCompatibility com Version=143920 é o "número mágico" do Gx18
// sem ele o GeneXus rejeita o package com "version 0, expecting 143920"
using System.Reflection;
using Artech.Architecture.Common.Packages;

[assembly: PackageCompatibility(Version = 143920)]

#if GX18_U14
[assembly: AssemblyMetadata("GxLine", "Gx18u14")]
#else
[assembly: AssemblyMetadata("GxLine", "Gx18u13")]
#endif
